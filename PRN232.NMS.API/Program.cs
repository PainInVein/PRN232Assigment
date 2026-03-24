using Grader.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232.NMS.API.Models.ResponseModels;
using PRN232.NMS.Repo.DBContext;
using PRN232.NMS.Services.BusinessModel;
using PRN232.NMS.Services.Helpers.HelperClasses;
using PRN232.NMS.Services.Models.MappingTool;
using PRN232.NMS.Services.Services;
using Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<GradingService>();
builder.Services.AddScoped<ExecuteTestService>();
builder.Services.AddScoped<FolderService>();
builder.Services.AddDbContext<Prn232lab3Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();

builder.Services.AddScoped<IClassHelperFacade, ClassHelperFacade>();
builder.Services.AddScoped<CopyDirectoryHelper>();
builder.Services.AddScoped<CreateDatabaseHelper>();
builder.Services.AddScoped<TryDropDatabaseHelper>();
builder.Services.AddScoped<ApplySchemaHelper>();
builder.Services.AddScoped<StudentConnectionStringHelper>();
builder.Services.AddScoped<ProjectBuildHelper>();
builder.Services.AddScoped<StartApiHelper>();
builder.Services.AddScoped<WaitForApiReadyHelper>();
builder.Services.AddScoped<TokenHelper>();
builder.Services.AddScoped<DiscoverRoutesHelper>();
builder.Services.AddScoped<GetTestSuiteHelper>();
builder.Services.AddScoped<GetFreePortHelper>();
builder.Services.AddScoped<WindowsPathToContainerHelper>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Customizing Model Validation Error Response
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value!.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var response = new ResponseDTO<object>()
        {
            Message = "Validation failed",
            IsSuccess = false,
            Data = null,
            Errors = errors
        };

        return new BadRequestObjectResult(response);
    };
});


//Mapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

var app = builder.Build();

//Global Exception Handling Middleware
app.UseMiddleware<PRN232.NMS.API.Middlewares.ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
