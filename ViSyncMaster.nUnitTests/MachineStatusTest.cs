using ViSyncMaster.DataModel;
using ViSyncMaster.ViewModels;
using ViSyncMaster.Views;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using ViSyncMaster;
using ViSyncMaster.Entitys;


namespace ViSyncMaster.nUnitTests
{
    public class MachineStatusTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var message = new MessagePgToSplunk();

            // Assert
            Assert.AreEqual("false", message.Producing);
            Assert.AreEqual("true", message.Waiting); // Default state
            Assert.AreEqual("false", message.Maintenance);
            Assert.AreEqual("false", message.Setting);
            Assert.AreEqual("false", message.Downtime);
        }

        [Test]
        public void SetByCounter_ShouldSetCorrectState()
        {
            // Arrange
            var message = new MessagePgToSplunk();

            // Act
            message.SetByCounter(1); // Producing
            Assert.AreEqual("true", message.Producing);
            Assert.AreEqual("false", message.Waiting);
            Assert.AreEqual("false", message.Maintenance);
            Assert.AreEqual("false", message.Setting);
            Assert.AreEqual("false", message.Downtime);

            message.SetByCounter(3); // Maintenance
            Assert.AreEqual("false", message.Producing);
            Assert.AreEqual("false", message.Waiting);
            Assert.AreEqual("true", message.Maintenance);
            Assert.AreEqual("false", message.Setting);
            Assert.AreEqual("false", message.Downtime);

            message.SetByCounter(2); // Waiting
            Assert.AreEqual("false", message.Producing);
            Assert.AreEqual("true", message.Waiting);
            Assert.AreEqual("false", message.Maintenance);
            Assert.AreEqual("false", message.Setting);
            Assert.AreEqual("false", message.Downtime);

            message.SetByCounter(4); // Setting
            Assert.AreEqual("false", message.Producing);
            Assert.AreEqual("false", message.Waiting);
            Assert.AreEqual("false", message.Maintenance);
            Assert.AreEqual("true", message.Setting);
            Assert.AreEqual("false", message.Downtime);

            message.SetByCounter(5); // Downtime
            Assert.AreEqual("false", message.Producing);
            Assert.AreEqual("false", message.Waiting);
            Assert.AreEqual("false", message.Maintenance);
            Assert.AreEqual("false", message.Setting);
            Assert.AreEqual("true", message.Downtime);
        }

        [Test]
        public void SetByCounter_ShouldThrowExceptionForInvalidCounter()
        {
            // Arrange
            var message = new MessagePgToSplunk();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => message.SetByCounter(0)); // Invalid counter
            Assert.Throws<ArgumentOutOfRangeException>(() => message.SetByCounter(6)); // Invalid counter
        }

        [Test]
        public void ResetValues_ShouldResetAllStatesToFalse()
        {
            // Arrange
            var message = new MessagePgToSplunk();
            message.SetByCounter(4); // Set a state (e.g., Setting = true)

            // Act
            var privateMethod = typeof(MessagePgToSplunk)
                .GetMethod("ResetValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            privateMethod.Invoke(message, null); // Call ResetValues using reflection (since it's private)

            // Assert
            Assert.AreEqual("false", message.Producing);
            Assert.AreEqual("false", message.Waiting);
            Assert.AreEqual("false", message.Maintenance);
            Assert.AreEqual("false", message.Setting);
            Assert.AreEqual("false", message.Downtime);
        }




    }
}


