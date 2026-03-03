using TaskGX.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Serviços
// ----------------------------
builder.Services.AddControllersWithViews();

// Sessão + HttpContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//  HTTP Client para chamar a API
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl não configurado no appsettings.json.");

    client.BaseAddress = new Uri(baseUrl);
});

// (Opcional, mas recomendado) Services que chamam a API
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<ListasApiService>();
builder.Services.AddScoped<TarefasApiService>();

//  Removidos: EmailSettings/EmailSender/RazorViewToStringRenderer/Repositorios/ServicoAutenticacao
// (porque agora o site vai consumir a API e não fazer lógica local)

// ----------------------------
// Build da aplicação
// ----------------------------
var app = builder.Build();

// ----------------------------
// Pipeline
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}"
);

app.Run();
