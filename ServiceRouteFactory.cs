using Microsoft.Extensions.DependencyInjection;

namespace BibleRecallTrainerV2;

public sealed class ServiceRouteFactory<TPage>(IServiceProvider services) : RouteFactory where TPage : Element
{
    public override Element GetOrCreate() => services.GetRequiredService<TPage>();

    public override Element GetOrCreate(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<TPage>();
}
