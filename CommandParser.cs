using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IpUtil;

[AttributeUsage(AttributeTargets.Method)]
public class Command : Attribute
{
    public string Args { get; set; }
    public string Alternative { get; set; }
    public string Additional { get; set; }
}

public class CommandParser
{
    public static readonly Dictionary<string, Action> Commands = new Dictionary<string, Action>();
    
    public static Task LoadCommands()
    {
        return Task.Run(() =>
        {
            var type = typeof(Program);
            foreach (var p in type.GetMethods())
            {
                foreach (var command in p.GetCustomAttributes(false).OfType<Command>())
                {
                    Commands.Add(command.Args, () => p.Invoke(null, null));

                    if (!string.IsNullOrEmpty(command.Alternative))
                        Commands.Add(command.Alternative, () => p.Invoke(null, null));
                }
            }
        });
    }
}