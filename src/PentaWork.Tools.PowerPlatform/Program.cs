using CliFx;

return await new CommandLineApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .SetTitle("pw-pp")
    .SetExecutableName("pw-pp")
    .Build()
    .RunAsync(args);
