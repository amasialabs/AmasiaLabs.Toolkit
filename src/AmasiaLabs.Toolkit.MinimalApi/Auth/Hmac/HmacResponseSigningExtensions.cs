using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

public static class HmacResponseSigningExtensions
{
    public static IApplicationBuilder UseHmacResponseSigning(this IApplicationBuilder app)
        => app.UseHmacResponseSigning(configure: null);

    // ReSharper disable once CognitiveComplexity
    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseHmacResponseSigning(this IApplicationBuilder app, Action<HmacResponseSigningOptions>? configure)
    {
        var defaultOptions = new HmacResponseSigningOptions();
        configure?.Invoke(defaultOptions);

        return app.Use(async (ctx, next) =>
        {
            if (!defaultOptions.ShouldSign(ctx))
            {
                await next();
                return;
            }

            // Buffer response body
            var originalBody = ctx.Response.Body;
            await using var buffer = new MemoryStream();
            ctx.Response.Body = buffer;

            try
            {
                await next();

                // Only sign if the status condition matches
                if (defaultOptions.SuccessOnly && ctx.Response.StatusCode is < 200 or >= 300)
                    return;

                buffer.Position = 0;
                using var reader = new StreamReader(buffer, defaultOptions.Encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var bodyText = await reader.ReadToEndAsync();

                var payload = defaultOptions.BuildPayload is not null
                    ? await defaultOptions.BuildPayload(ctx, bodyText)
                    : bodyText;

                var clientId = defaultOptions.ResolveClientId(ctx.User);
                if (string.IsNullOrWhiteSpace(clientId))
                    return;

                var keyProvider = ctx.RequestServices.GetService<IHmacKeyProvider>();
                var signer = ctx.RequestServices.GetService<IHmacSignatureSigner>();
                if (keyProvider is null || signer is null)
                    return;

                var key = await keyProvider.GetKeyAsync(clientId, ctx.RequestAborted);
                if (string.IsNullOrWhiteSpace(key))
                    return;

                var signature = signer.ComputeSignature(key, payload);
                ctx.Response.Headers[defaultOptions.HeaderName] = signature;
            }
            finally
            {
                // Copy buffer to the original stream
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
                ctx.Response.Body = originalBody;
            }
        });
    }
}
