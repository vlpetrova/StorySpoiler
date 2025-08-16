using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;

namespace StorySpoiler
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient client;
        private static string? storyId;
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("harpvl", "zxcvbn");
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var authClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new
            {
                username,
                password
            });
            var response = authClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new
            {
                Title = "New Story",
                Description = "History",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            storyId = json.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.Created), "Expected status code 201.");
                Assert.That(storyId, Is.Not.Null.And.Not.Empty, "Story ID should not be null or empty.");
                Assert.That(response.Content, Does.Contain("Successfully created!"));
            });
        }

        [Test, Order(2)]
        public void EditStoryTitle_ShouldReturnOK()
        {
            var editedStory = new StoryDTO()
            {
                Title = "Edited Story",
                Description = "Edited History",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{storyId}", Method.Put);
            request.AddJsonBody(editedStory);

            var response = client.Execute(request);
            var json = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }
        [Test, Order(3)]
        public void GetAllStorySpoilers_ShouldReturnList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));

            var stories = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(stories, Is.Not.Empty);
        }
        [Test, Order(4)]
        public void DeleteStorySpoiler_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{storyId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]

        public void CreateStorySpoiler_WithoutRequiredFields_ShouldReturdBadrequest()

        {
            var story = new
            {
                Name = "",
                Description = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]

        public void EditNonExistingStorySpoiler_ShouldReturnNotFound()
        {
            string fakeID = "1234567";

            var editedStory = new StoryDTO()
            {
                Title = "Edited Story",
                Description = "Edited History",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{fakeID}", Method.Put);
            request.AddJsonBody(editedStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));

        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            string fakeID = "123456";

            var request = new RestRequest($"/api/Story/Delete/{fakeID}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}