using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VoxieQuest.Web;
using VoxieQuest.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HTTP Client for loading static educational curriculum JSON files
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Register Core Application & Child-Safe Storage Services
builder.Services.AddScoped<ISaveService, SaveService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IAdaptiveDifficultyService, AdaptiveDifficultyService>();
builder.Services.AddScoped<IDifficultyService, DifficultyService>();
builder.Services.AddScoped<IGameStateService, GameStateService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddScoped<IAccessibilityService, AccessibilityService>();
builder.Services.AddScoped<ISessionTimerService, SessionTimerService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<IClassroomService, ClassroomService>();
builder.Services.AddScoped<IIndiaMapService, IndiaMapService>();
builder.Services.AddScoped<IWeeklyLeagueService, WeeklyLeagueService>();

// Register Voice Subsystem & Pure C# Speech Matcher
builder.Services.AddSingleton<ISpeechMatcher, SpeechMatcher>();
builder.Services.AddScoped<BrowserSpeechVoiceService>();
builder.Services.AddScoped<OfflineVoiceService>();
builder.Services.AddScoped<VoiceManager>();
builder.Services.AddScoped<IVoiceService>(sp => sp.GetRequiredService<VoiceManager>());

await builder.Build().RunAsync();
