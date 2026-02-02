using TaskGX.Data;
using TaskGX.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Serviços
// ----------------------------
builder.Services.AddControllersWithViews();

// Injeção de dependência
builder.Services.AddScoped<RepositorioUsuario>();
builder.Services.AddScoped<RepositorioDashboard>();
builder.Services.AddScoped<RepositorioTarefas>();
builder.Services.AddScoped<ServicoAutenticacao>();

// Sessão
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
app.UseSession(); // Sessão precisa vir antes de MapControllerRoute

// Mapear rotas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}"
);

app.Run();