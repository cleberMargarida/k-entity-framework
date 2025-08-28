using K.EntityFrameworkCore.Extensions;
using System.Reflection;
using Xunit;

namespace K.EntityFrameworkCore.UnitTests.Extensions
{
    public class ServiceAttributesTests
    {
        [ScopedService]
        private class TestScopedService
        {
        }

        [SingletonService]
        private class TestSingletonService
        {
        }

        private class TestNormalService
        {
        }

        [Fact]
        public void ScopedServiceAttribute_ShouldBeDefinedCorrectly()
        {
            // Arrange
            var attribute = typeof(ScopedServiceAttribute);

            // Assert
            Assert.True(attribute.IsSubclassOf(typeof(Attribute)));
            
            var usage = attribute.GetCustomAttribute<AttributeUsageAttribute>();
            Assert.NotNull(usage);
            Assert.Equal(AttributeTargets.Class, usage.ValidOn);
            Assert.False(usage.Inherited);
            Assert.False(usage.AllowMultiple);
        }

        [Fact]
        public void SingletonServiceAttribute_ShouldBeDefinedCorrectly()
        {
            // Arrange
            var attribute = typeof(SingletonServiceAttribute);

            // Assert
            Assert.True(attribute.IsSubclassOf(typeof(Attribute)));
            
            var usage = attribute.GetCustomAttribute<AttributeUsageAttribute>();
            Assert.NotNull(usage);
            Assert.Equal(AttributeTargets.Class, usage.ValidOn);
            Assert.False(usage.Inherited);
            Assert.False(usage.AllowMultiple);
        }

        [Fact]
        public void ScopedServiceAttribute_CanBeAppliedToClass()
        {
            // Act
            var attributes = typeof(TestScopedService).GetCustomAttributes<ScopedServiceAttribute>();

            // Assert
            Assert.Single(attributes);
        }

        [Fact]
        public void SingletonServiceAttribute_CanBeAppliedToClass()
        {
            // Act
            var attributes = typeof(TestSingletonService).GetCustomAttributes<SingletonServiceAttribute>();

            // Assert
            Assert.Single(attributes);
        }

        [Fact]
        public void ServiceAttributes_ShouldNotBeAppliedToNormalClass()
        {
            // Act
            var scopedAttributes = typeof(TestNormalService).GetCustomAttributes<ScopedServiceAttribute>();
            var singletonAttributes = typeof(TestNormalService).GetCustomAttributes<SingletonServiceAttribute>();

            // Assert
            Assert.Empty(scopedAttributes);
            Assert.Empty(singletonAttributes);
        }

        [Fact]
        public void ScopedServiceAttribute_ShouldBeInternal()
        {
            // Arrange
            var attribute = typeof(ScopedServiceAttribute);

            // Assert
            Assert.True(attribute.IsNotPublic);
        }

        [Fact]
        public void SingletonServiceAttribute_ShouldBeInternal()
        {
            // Arrange
            var attribute = typeof(SingletonServiceAttribute);

            // Assert
            Assert.True(attribute.IsNotPublic);
        }

        [Fact]
        public void ScopedServiceAttribute_ShouldHaveParameterlessConstructor()
        {
            // Act
            var constructors = typeof(ScopedServiceAttribute).GetConstructors();

            // Assert
            Assert.Single(constructors);
            Assert.Empty(constructors[0].GetParameters());
        }

        [Fact]
        public void SingletonServiceAttribute_ShouldHaveParameterlessConstructor()
        {
            // Act
            var constructors = typeof(SingletonServiceAttribute).GetConstructors();

            // Assert
            Assert.Single(constructors);
            Assert.Empty(constructors[0].GetParameters());
        }

        [Fact]
        public void ScopedServiceAttribute_ShouldNotBeSealed()
        {
            // Arrange
            var attribute = typeof(ScopedServiceAttribute);

            // Assert
            Assert.False(attribute.IsSealed);
        }

        [Fact]
        public void SingletonServiceAttribute_ShouldNotBeSealed()
        {
            // Arrange
            var attribute = typeof(SingletonServiceAttribute);

            // Assert
            Assert.False(attribute.IsSealed);
        }

        [Fact]
        public void ScopedServiceAttribute_Instance_ShouldNotBeNull()
        {
            // Act
            var instance = new ScopedServiceAttribute();

            // Assert
            Assert.NotNull(instance);
            Assert.IsType<ScopedServiceAttribute>(instance);
        }

        [Fact]
        public void SingletonServiceAttribute_Instance_ShouldNotBeNull()
        {
            // Act
            var instance = new SingletonServiceAttribute();

            // Assert
            Assert.NotNull(instance);
            Assert.IsType<SingletonServiceAttribute>(instance);
        }
    }
}
