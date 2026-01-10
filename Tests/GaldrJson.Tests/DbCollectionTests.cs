using GaldrJson.Tests.Models;

namespace GaldrJson.Tests
{
    [TestClass]
    public sealed class DbCollectionTests
    {
        #region GaldrDbCollection Tests

        [TestMethod]
        public void DbCollection_SerializeAndDeserialize()
        {
            var model = new DbCollectionTestModel
            {
                Id = 42,
                Name = "Test Item"
            };

            string json = GaldrJson.Serialize(model);
            var deserialized = GaldrJson.Deserialize<DbCollectionTestModel>(json);

            Assert.IsNotNull(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(model.Id, deserialized.Id);
            Assert.AreEqual(model.Name, deserialized.Name);
        }

        [TestMethod]
        public void DbCollectionWithName_SerializeAndDeserialize()
        {
            var model = new DbCollectionWithNameTestModel
            {
                UserId = 123,
                Username = "johndoe",
                Email = "john@example.com"
            };

            string json = GaldrJson.Serialize(model);
            var deserialized = GaldrJson.Deserialize<DbCollectionWithNameTestModel>(json);

            Assert.IsNotNull(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(model.UserId, deserialized.UserId);
            Assert.AreEqual(model.Username, deserialized.Username);
            Assert.AreEqual(model.Email, deserialized.Email);
        }

        #endregion

        #region GaldrDbProjection Tests

        [TestMethod]
        public void DbProjection_SerializeAndDeserialize()
        {
            var model = new UserProjectionModel
            {
                Id = 1,
                Username = "testuser"
            };

            string json = GaldrJson.Serialize(model);
            var deserialized = GaldrJson.Deserialize<UserProjectionModel>(json);

            Assert.IsNotNull(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(model.Id, deserialized.Id);
            Assert.AreEqual(model.Username, deserialized.Username);
        }

        [TestMethod]
        public void DbProjection_DeserializeFromFullModel()
        {
            // JSON from a full model can be deserialized into a projection
            var fullModel = new FullUserModel
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "abc123",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0)
            };

            string fullJson = GaldrJson.Serialize(fullModel);
            var projection = GaldrJson.Deserialize<UserProjectionModel>(fullJson);

            Assert.IsNotNull(projection);
            Assert.AreEqual(fullModel.Id, projection.Id);
            Assert.AreEqual(fullModel.Username, projection.Username);
        }

        [TestMethod]
        public void FullUserModel_WithDbCollection_SerializeAndDeserialize()
        {
            var model = new FullUserModel
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0)
            };

            string json = GaldrJson.Serialize(model);
            var deserialized = GaldrJson.Deserialize<FullUserModel>(json);

            Assert.IsNotNull(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(model.Id, deserialized.Id);
            Assert.AreEqual(model.Username, deserialized.Username);
            Assert.AreEqual(model.Email, deserialized.Email);
            Assert.AreEqual(model.PasswordHash, deserialized.PasswordHash);
            Assert.AreEqual(model.CreatedAt, deserialized.CreatedAt);
        }

        #endregion
    }
}
