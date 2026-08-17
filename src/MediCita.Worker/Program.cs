using MediCita.Application;
using MediCita.Infrastructure;
using MediCita.Worker;

var constructor = Host.CreateApplicationBuilder(args);

constructor.Services.AgregarAplicacion();
constructor.Services.AgregarInfraestructura(constructor.Configuration);
constructor.Services.AddHostedService<TareaDeRecordatorios>();

var host = constructor.Build();
host.Run();
