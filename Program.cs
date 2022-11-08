

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.RemoveType<HeaderModelBinderProvider>();
    options.ModelBinderProviders.Insert(0, new CustomHeaderModelBinderProvider());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public class CustomHeaderModelBinder : IModelBinder
{
    public CustomHeaderModelBinder(IModelBinder innerModelBinder)
    {
        InnerModelBinder = innerModelBinder;
    }

    private IModelBinder? InnerModelBinder { get; }

    /// <inheritdoc /> 
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var headerKey = bindingContext.ModelMetadata.BinderModelName;
        if (string.IsNullOrEmpty(headerKey))
        {
            await InnerModelBinder.BindModelAsync(bindingContext);
        }

        bindingContext.HttpContext.Request.Headers.TryGetValue(headerKey, out var headerValue);
        var modelType = bindingContext.ModelMetadata.ModelType;
        if (!string.IsNullOrEmpty(headerValue))
        {
            var result = headerValue
                .SelectMany(x => x.Split(new[] { ' ', ','}, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();
            bindingContext.Model = result;
            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
        }
        await Task.CompletedTask;
    }
}

public class CustomHeaderModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var x = context.Metadata as DefaultModelMetadata;
        var headerAttribute = x.Attributes.Attributes
            .Where(a => a.GetType() == typeof(FromHeaderAttribute))
            .FirstOrDefault();
        if (headerAttribute != null)
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new CustomHeaderModelBinder(new HeaderModelBinder(loggerFactory));
        }

        return null;
    }
}

public partial class Program { }