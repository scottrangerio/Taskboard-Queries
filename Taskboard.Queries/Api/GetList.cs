using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using Taskboard.Queries.DTO;
using Taskboard.Queries.Exceptions;
using Taskboard.Queries.Extensions;
using Taskboard.Queries.Handlers;
using Taskboard.Queries.Queries;

namespace Taskboard.Queries.Api
{
    public static class GetList
    {
        public static Container Container = BuildContainer();

        [FunctionName(nameof(GetList))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            try
            {
                var query = new GetListQuery {Id = id};
                var handler = Container.GetInstance<IQueryHandler<GetListQuery, ListDTO>>();

                var result = await handler.Execute(query);

                return new OkObjectResult(result);
            }
            catch (ResourceNotFoundException ex)
            {
                Container.GetInstance<TelemetryClient>().TrackException(ex);

                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                Container.GetInstance<TelemetryClient>().TrackException(ex);

                return new InternalServerErrorResult();
            }
        }

        private static Container BuildContainer()
        {
            var container = new Container();

            container.WithTelemetryClient();
            container.WithDocumentClient();
            container.Register<IQueryHandler<GetListQuery, ListDTO>>(() => new GetListQueryHandler(
                container.GetInstance<IDocumentClient>(),
                Environment.GetEnvironmentVariable("COSMOS_DB"),
                Environment.GetEnvironmentVariable("COSMOS_COLLECTION")));

            return container;
        }
    }
}