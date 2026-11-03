using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameJam2026.Abilities.Tests
{
    public sealed class DashAbilityDataTests
    {
        [TestCase(0.2f, true)]
        [TestCase(0.001f, true)]
        [TestCase(0f, false)]
        [TestCase(-0.2f, false)]
        [TestCase(float.NaN, false)]
        [TestCase(float.PositiveInfinity, false)]
        [TestCase(float.NegativeInfinity, false)]
        public void DurationValidation_RequiresFinitePositiveValue(float duration, bool valid)
        {
            DashAbilityData original = AssetDatabase.LoadAssetAtPath<DashAbilityData>(
                "Assets/Data/Abilities/Dash_Test.asset");
            Assert.That(original, Is.Not.Null);
            DashAbilityData temporary = Object.Instantiate(original);
            try
            {
                var serialized = new SerializedObject(temporary);
                serialized.FindProperty("duration").floatValue = duration;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(temporary.TryValidate(out string error), Is.EqualTo(valid));
                if (valid)
                    Assert.That(error, Is.Null);
                else
                    Assert.That(error, Does.Contain("duration"));
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }
        }

        [Test]
        public void CurrentDashAsset_ExposesDistanceAndDurationForExecutionAndDetails()
        {
            DashAbilityData dash = AssetDatabase.LoadAssetAtPath<DashAbilityData>(
                "Assets/Data/Abilities/Dash_Test.asset");
            Assert.That(dash, Is.Not.Null);
            Assert.That(dash.TryValidate(out string error), Is.True, error);
            Assert.That(dash.SlotCost, Is.EqualTo(4));
            Assert.That(dash.Distance, Is.EqualTo(4f));
            Assert.That(dash.Duration, Is.EqualTo(0.2f));
            Assert.That(dash.GetParameters().Count, Is.EqualTo(2));
            Assert.That(dash.GetParameters()[0].DisplayName, Is.EqualTo("冲刺距离"));
            Assert.That(dash.GetParameters()[0].Value, Is.EqualTo(dash.Distance));
            Assert.That(dash.GetParameters()[1].DisplayName, Is.EqualTo("冲刺持续时间"));
            Assert.That(dash.GetParameters()[1].Value, Is.EqualTo(dash.Duration));
        }
    }
}
