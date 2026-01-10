namespace GaldrJson.Tests.Models
{
    [GaldrDbCollection]
    internal class DbCollectionTestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [GaldrDbCollection("users")]
    internal class DbCollectionWithNameTestModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }
}
