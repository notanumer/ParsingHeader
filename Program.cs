using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


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

        var headerName = bindingContext.FieldName;
        var headerValueProvider = GetHeaderValueProvider(headerName, bindingContext);
        var isTopLevelObject = bindingContext.IsTopLevelObject;
        ModelBindingResult result;
        using (bindingContext.EnterNestedScope(
                bindingContext.ModelMetadata,
                fieldName: bindingContext.FieldName,
                modelName: bindingContext.ModelName,
                model: bindingContext.Model))
        {
            bindingContext.IsTopLevelObject = isTopLevelObject;
            bindingContext.ValueProvider = headerValueProvider;

            await InnerModelBinder.BindModelAsync(bindingContext);
            result = bindingContext.Result;
        }

        bindingContext.Result = result;
    }

    private CustomHeaderValueProvider GetHeaderValueProvider(string headerName, ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;

        var values = Array.Empty<string>();
        if (request.Headers.ContainsKey(headerName))
        {
            if (bindingContext.ModelMetadata.IsEnumerableType)
            {
                values = request.Headers[headerName]
                    .SelectMany(x => x.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .ToArray();
            }
            else
            {
                values = new[] { (string)request.Headers[headerName] };
            }
        }
        return new CustomHeaderValueProvider(values);
    }

    private class CustomHeaderValueProvider : IValueProvider
    {
        private readonly string[] _values;

        public CustomHeaderValueProvider(string[] values)
        {
            Debug.Assert(values != null);

            _values = values;
        }

        public bool ContainsPrefix(string prefix)
        {
            return _values.Length != 0;
        }

        public ValueProviderResult GetValue(string key)
        {
            if (_values.Length == 0)
            {
                return ValueProviderResult.None;
            }
            else
            {
                return new ValueProviderResult(_values, CultureInfo.InvariantCulture);
            }
        }
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

        var bindingInfo = context.BindingInfo;
        if (bindingInfo.BindingSource == null ||
            !bindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Header))
        {
            return null;
        }

        var modelMetadata = context.Metadata; 

        var nestedBindingInfo = new BindingInfo(bindingInfo)
        {
            BindingSource = BindingSource.ModelBinding
        };

        var innerModelBinder = context.CreateBinder(
                modelMetadata.GetMetadataForType(modelMetadata.ModelType),
                nestedBindingInfo);

        if (innerModelBinder == null)
        {
            return null;
        }

        return new CustomHeaderModelBinder(innerModelBinder);
    }
}

public partial class Program { }