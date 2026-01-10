namespace GaldrJson.Tests.Models
{
    [GaldrDbCollection]
    internal class FullUserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [GaldrDbProjection(typeof(FullUserModel))]
    internal class UserProjectionModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }
}
