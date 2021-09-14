using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;

namespace YouTubeFunctions
{
    public static class SearchVideos
    {
        [FunctionName("SearchVideos")]
        [OpenApiOperation(operationId: "Search", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "query", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "skip", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiParameter(name: "take", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(IList<YouTubeChannel>))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string query = req.Query[nameof(query)];
            int skip = int.TryParse(req.Query[nameof(skip)], out skip) ? skip : 0;
            int take = int.TryParse(req.Query[nameof(take)], out take) ? take : 50;

            return new OkObjectResult(await Search(query, skip, take));
        }

        private static async Task<IList<YouTubeChannel>> Search(string query, int skip, int take)
        {
            var results = await new YoutubeClient().Search.GetChannelsAsync(query);

            return results.Select(Convert).Skip(skip).Take(take).ToList();
        }

        private static YouTubeChannel Convert(IChannel channel)
        {
            var youTubeChannel = new YouTubeChannel(channel.Id.Value, channel.Title, channel.Url);

            var thumbnail = channel.Thumbnails
                .OrderByDescending(t => t.Resolution.Width)
                .FirstOrDefault();

            youTubeChannel.ThumbnailSize = thumbnail?.Resolution.Width;
            youTubeChannel.ThumbnailUrl = thumbnail?.Url;

            return youTubeChannel;
        }
    }

    public class YouTubeChannel
    {
        public string ChannelId { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public int? ThumbnailSize { get; set; }

        public string? ThumbnailUrl { get; set; }

        public YouTubeChannel(string channelId, string title, string url)
        {
            (ChannelId, Title, Url) = (channelId, title, url);
        }
    }
}

