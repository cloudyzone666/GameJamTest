using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameJam2026.Abilities.Tests
{
    public sealed class AbilityManagerTests
    {
        private readonly List<Object> temporaryObjects = new List<Object>();
        private AbilitySystemConfig config;
        private JumpAbilityData jump;
        private DashAbilityData dash;
        private AbilityManager manager;

        [SetUp]
        public void SetUp()
        {
            config = Load<AbilitySystemConfig>("DefaultAbilitySystemConfig");
            jump = Load<JumpAbilityData>("Jump_Test");
            dash = Load<DashAbilityData>("Dash_Test");
            manager = new AbilityManager(config);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object temporary in temporaryObjects)
                Object.DestroyImmediate(temporary);
            temporaryObjects.Clear();
        }

        [Test]
        public void NewManager_ReadsIndependentLimitsAndStartsEmpty()
        {
            Assert.That(manager.TotalCapacity, Is.EqualTo(12));
            Assert.That(manager.MaxEquippedCount, Is.EqualTo(4));
            AssertState(0, 0, 12);
        }

        [TestCase(4, 0, 8, 4)]
        [TestCase(0, 3, 12, 0)]
        [TestCase(2, 2, 12, 0)]
        public void RequestedLoadouts_TrackCountAndCapacity(int jumps, int dashes,
            int used, int remaining)
        {
            for (int i = 0; i < jumps; i++) Equip(jump);
            for (int i = 0; i < dashes; i++) Equip(dash);
            AssertState(jumps + dashes, used, remaining);
            Assert.That(manager.GetEquippedCount(jump), Is.EqualTo(jumps));
            Assert.That(manager.GetEquippedCount(dash), Is.EqualTo(dashes));
        }

        [Test]
        public void FifthJump_IsRejectedWithCapacityStillAvailable()
        {
            for (int i = 0; i < 4; i++) Equip(jump);
            AssertRejected(jump, AbilityEquipResult.MaxEquippedCountReached);
            AssertState(4, 8, 4);
        }

        [Test]
        public void FourthDash_IsRejectedWithInstancePositionStillAvailable()
        {
            for (int i = 0; i < 3; i++) Equip(dash);
            AssertRejected(dash, AbilityEquipResult.NotEnoughCapacity);
            AssertState(3, 12, 0);
        }

        [Test]
        public void DuplicateDefinitions_CreateDifferentRuntimeInstances()
        {
            EquippedAbility first = Equip(jump);
            EquippedAbility second = Equip(jump);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Ability, Is.SameAs(jump));
            Assert.That(second.Ability, Is.SameAs(jump));
            Assert.That(first.InstanceId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(second.InstanceId, Is.Not.EqualTo(first.InstanceId));
        }

        [Test]
        public void UnequipById_RemovesOnlyChosenCopyAndRefundsItsCost()
        {
            EquippedAbility first = Equip(jump);
            EquippedAbility second = Equip(jump);
            EquippedAbility third = Equip(dash);
            Assert.That(manager.TryUnequip(first.InstanceId, out EquippedAbility removed), Is.True);
            Assert.That(removed, Is.SameAs(first));
            Assert.That(manager.EquippedAbilities, Is.EqualTo(new[] { second, third }));
            AssertState(2, 6, 6);
        }

        [Test]
        public void UnequipLast_RemovesLatestMatchingCopyAcrossOtherAbilities()
        {
            EquippedAbility first = Equip(jump);
            EquippedAbility lastJump = Equip(jump);
            EquippedAbility lastOverall = Equip(dash);
            Assert.That(manager.TryUnequipLast(jump, out EquippedAbility removed), Is.True);
            Assert.That(removed, Is.SameAs(lastJump));
            Assert.That(manager.EquippedAbilities, Is.EqualTo(new[] { first, lastOverall }));
            AssertState(2, 6, 6);
        }

        [Test]
        public void MissingInstancesAndDefinitions_DoNotChangeStateOrNotify()
        {
            Equip(jump);
            int notifications = 0;
            manager.EquipmentChanged += () => notifications++;
            Assert.That(manager.TryUnequip(Guid.Empty, out EquippedAbility removed), Is.False);
            Assert.That(removed, Is.Null);
            Assert.That(manager.TryUnequip(Guid.NewGuid(), out removed), Is.False);
            Assert.That(removed, Is.Null);
            Assert.That(manager.TryUnequipLast(dash, out removed), Is.False);
            Assert.That(removed, Is.Null);
            Assert.That(manager.TryUnequipLast(null, out removed), Is.False);
            Assert.That(removed, Is.Null);
            Assert.That(manager.GetEquippedCount(null), Is.Zero);
            Assert.That(notifications, Is.Zero);
            AssertState(1, 2, 10);
        }

        [Test]
        public void NullAbility_IsRejectedWithoutMutation()
        {
            AssertRejected(null, AbilityEquipResult.InvalidAbility);
        }

        [TestCase("slotCost", -1)]
        [TestCase("abilityId", "")]
        [TestCase("displayName", "")]
        [TestCase("verticalHeight", -1f)]
        public void InvalidAbility_IsRejectedWithoutMutation(string fieldName, object value)
        {
            JumpAbilityData invalid = Clone(jump);
            SetField(invalid, fieldName, value);
            Assert.That(invalid.TryValidate(out _), Is.False);
            AssertRejected(invalid, AbilityEquipResult.InvalidAbility);
        }

        [Test]
        public void SuccessfulMutations_NotifyOnceAfterCompleteStateUpdate()
        {
            var states = new List<string>();
            manager.EquipmentChanged += () => states.Add(
                manager.EquippedCount + ":" + manager.UsedCapacity + ":" + manager.RemainingCapacity);
            EquippedAbility first = Equip(jump);
            Equip(dash);
            Assert.That(manager.TryUnequip(first.InstanceId, out _), Is.True);
            Assert.That(manager.TryUnequipLast(dash, out _), Is.True);
            Assert.That(states, Is.EqualTo(new[] { "1:2:10", "2:6:6", "1:4:8", "0:0:12" }));
        }

        [Test]
        public void RemovedSubscriber_DoesNotReceiveLaterChanges()
        {
            int notifications = 0;
            Action listener = () => notifications++;
            manager.EquipmentChanged += listener;
            Equip(jump);
            manager.EquipmentChanged -= listener;
            Equip(dash);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void ChangedDefinitionCost_RefundsOriginalChargedAmount()
        {
            JumpAbilityData copy = Clone(jump);
            EquippedAbility first = Equip(copy);
            SetField(copy, "slotCost", 7);
            EquippedAbility second = Equip(copy);
            Assert.That(first.ChargedSlotCost, Is.EqualTo(2));
            Assert.That(second.ChargedSlotCost, Is.EqualTo(7));
            Assert.That(manager.TryUnequip(first.InstanceId, out _), Is.True);
            AssertState(1, 7, 5);
            Assert.That(manager.TryUnequipLast(copy, out _), Is.True);
            AssertState(0, 0, 12);
        }

        [Test]
        public void InvalidatedDefinition_CanStillBeUnequipped()
        {
            JumpAbilityData copy = Clone(jump);
            Equip(copy);
            SetField(copy, "slotCost", -1);
            Assert.That(copy.TryValidate(out _), Is.False);
            Assert.That(manager.TryUnequipLast(copy, out _), Is.True);
            AssertState(0, 0, 12);
        }

        [Test]
        public void DestroyedDefinition_CanStillBeUnequippedByInstanceId()
        {
            JumpAbilityData copy = Clone(jump);
            EquippedAbility instance = Equip(copy);
            temporaryObjects.Remove(copy);
            Object.DestroyImmediate(copy);
            Assert.That(manager.TryUnequip(instance.InstanceId, out _), Is.True);
            AssertState(0, 0, 12);
        }

        [Test]
        public void ConfigChanges_ApplyToNewManagersAndKeepActiveLimitsStable()
        {
            AbilitySystemConfig copy = CloneConfig(9, 2);
            manager = new AbilityManager(copy);
            SetField(copy, "totalCapacity", 20);
            SetField(copy, "maxEquippedCount", 5);
            Equip(dash);
            Equip(dash);
            AssertRejected(jump, AbilityEquipResult.MaxEquippedCountReached);
            AssertState(2, 8, 1);
            Assert.That(manager.TotalCapacity, Is.EqualTo(9));
            var next = new AbilityManager(copy);
            Assert.That(next.TotalCapacity, Is.EqualTo(20));
            Assert.That(next.MaxEquippedCount, Is.EqualTo(5));
        }

        [Test]
        public void ZeroCost_StillUsesAnInstancePositionWithZeroCapacity()
        {
            manager = new AbilityManager(CloneConfig(0, 2));
            JumpAbilityData freeJump = Clone(jump);
            SetField(freeJump, "slotCost", 0);
            Equip(freeJump);
            Equip(freeJump);
            AssertRejected(freeJump, AbilityEquipResult.MaxEquippedCountReached);
            AssertState(2, 0, 0);
        }

        [Test]
        public void ZeroInstanceLimit_TakesPriorityWhenBothLimitsFail()
        {
            manager = new AbilityManager(CloneConfig(0, 0));
            AssertRejected(jump, AbilityEquipResult.MaxEquippedCountReached);
        }

        [Test]
        public void MaximumIntegerCapacity_DoesNotOverflow()
        {
            manager = new AbilityManager(CloneConfig(int.MaxValue, 2));
            JumpAbilityData huge = Clone(jump);
            SetField(huge, "slotCost", int.MaxValue);
            EquippedAbility instance = Equip(huge);
            AssertRejected(jump, AbilityEquipResult.NotEnoughCapacity);
            Assert.That(manager.UsedCapacity, Is.EqualTo(int.MaxValue));
            Assert.That(manager.TryUnequip(instance.InstanceId, out _), Is.True);
            AssertState(0, 0, int.MaxValue);
        }

        [Test]
        public void ReadOnlyCollection_CannotBeChangedByConsumers()
        {
            EquippedAbility instance = Equip(jump);
            var collection = (ICollection<EquippedAbility>)manager.EquippedAbilities;
            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection.Clear());
            Assert.Throws<NotSupportedException>(() => collection.Add(instance));
            AssertState(1, 2, 10);
        }

        [Test]
        public void Preview_DoesNotChangeEquipmentOrNotify()
        {
            int notifications = 0;
            manager.EquipmentChanged += () => notifications++;
            Assert.That(manager.CheckEquip(jump), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(notifications, Is.Zero);
            AssertState(0, 0, 12);
        }

        [Test]
        public void NullConfig_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new AbilityManager(null));
        }

        [TestCase("totalCapacity")]
        [TestCase("maxEquippedCount")]
        public void InvalidConfig_IsRejected(string fieldName)
        {
            AbilitySystemConfig invalid = Clone(config);
            SetField(invalid, fieldName, -1);
            Assert.That(invalid.TryValidate(out _), Is.False);
            Assert.Throws<ArgumentException>(() => new AbilityManager(invalid));
        }

        private static T Load<T>(string name) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>("Assets/Data/Abilities/" + name + ".asset");
            Assert.That(asset, Is.Not.Null, "Expected existing phase-one configuration resource.");
            return asset;
        }

        private T Clone<T>(T original) where T : Object
        {
            T copy = Object.Instantiate(original);
            temporaryObjects.Add(copy);
            return copy;
        }

        private AbilitySystemConfig CloneConfig(int totalCapacity, int maxEquippedCount)
        {
            AbilitySystemConfig copy = Clone(config);
            SetField(copy, "totalCapacity", totalCapacity);
            SetField(copy, "maxEquippedCount", maxEquippedCount);
            return copy;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            Assert.That(EditorUtility.IsPersistent(target), Is.False,
                "Test overrides must never modify a project asset.");
            using (var serialized = new SerializedObject(target))
            {
                SerializedProperty property = serialized.FindProperty(fieldName);
                Assert.That(property, Is.Not.Null, "Missing serialized test field: " + fieldName);
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = Convert.ToInt32(value);
                        break;
                    case SerializedPropertyType.Float:
                        property.floatValue = Convert.ToSingle(value);
                        break;
                    case SerializedPropertyType.String:
                        property.stringValue = (string)value;
                        break;
                    default:
                        Assert.Fail("Unsupported test field type: " + property.propertyType);
                        break;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        Assert.That(property.intValue, Is.EqualTo(Convert.ToInt32(value)));
                        break;
                    case SerializedPropertyType.Float:
                        Assert.That(property.floatValue, Is.EqualTo(Convert.ToSingle(value)));
                        break;
                    case SerializedPropertyType.String:
                        Assert.That(property.stringValue, Is.EqualTo((string)value));
                        break;
                }
            }
        }

        private EquippedAbility Equip(AbilityData ability)
        {
            Assert.That(manager.TryEquip(ability, out EquippedAbility instance),
                Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(instance, Is.Not.Null);
            return instance;
        }

        private void AssertRejected(AbilityData ability, AbilityEquipResult expected)
        {
            int beforeCount = manager.EquippedCount;
            int beforeUsed = manager.UsedCapacity;
            int beforeRemaining = manager.RemainingCapacity;
            var beforeInstances = new List<EquippedAbility>(manager.EquippedAbilities);
            int notifications = 0;
            Action listener = () => notifications++;
            manager.EquipmentChanged += listener;
            try
            {
                Assert.That(manager.CheckEquip(ability), Is.EqualTo(expected));
                Assert.That(manager.TryEquip(ability, out EquippedAbility instance), Is.EqualTo(expected));
                Assert.That(instance, Is.Null);
                Assert.That(manager.EquippedAbilities, Is.EqualTo(beforeInstances));
                AssertState(beforeCount, beforeUsed, beforeRemaining);
                Assert.That(notifications, Is.Zero);
            }
            finally
            {
                manager.EquipmentChanged -= listener;
            }
        }

        private void AssertState(int count, int used, int remaining)
        {
            Assert.That(manager.EquippedCount, Is.EqualTo(count));
            Assert.That(manager.EquippedAbilities.Count, Is.EqualTo(count));
            Assert.That(manager.UsedCapacity, Is.EqualTo(used));
            Assert.That(manager.RemainingCapacity, Is.EqualTo(remaining));
        }
    }
}
