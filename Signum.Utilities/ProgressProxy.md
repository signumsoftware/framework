# ProgressProxy 
In the same way throwing an `Exception` is an agnostic way of communicating errors, so you can throw an exception from 'pure' logic assembly and it can be cached and displayed in different ways in a ASP.Net, WinForms or WPF application, `ProgressProxy` class tries to make a agnostic way of communicating progress (to the user interface, presumably). 

`ProgressProxy` allows both a numerical (min, max, position, for a `ProgressBar` usually) way of indicating progress and a textual one ("Removing C:\Windows folder", "Fixing Registry Mess",...)

```C#
public class ProgressProxy
{
    //Event that notifies to the user interface about progress changes
    public event EventHandler<ProgressArgs> Changed;
   
    //Values for a ProgressBar
    public int Min {get;}
    public int Max {get;}    
    public int Position {get; set;} //-1 if no numeric progress enabled
  
    //Textural representation of the current task
    public string CurrentTask {get; set;}
    
    //Methods to initiate a new task
    public void Start(int max)
    public void Start(string currentTask)
    public void Start(int max, string currentTask)
    public void Start(int min, int max, string currentTask, int? position = null)

    //Sets currentTask and icrements position 1 (otherwise use CurrentTask property setter)
    public void NextTask(string currentTask)
    //Sets position and currentTask throwing just one event
    public void NextTask(int position, string currentTask)

    public void Reset()
}

public class ProgressArgs : EventArgs
{
    public readonly ProgressAction Action;
    public ProgressArgs(ProgressAction a)
}

public enum ProgressAction
{
    Interval = 1, //When Min and Max has changed
    Position = 2, //When Position have changed
    Task = 4, //When current task have changed
}
```

Example: 


```C#
//Pure logic code
private static void Sleep(ProgressProxy pp)
{
    pp.Start("Getting Into bed"); 
    pp.Start(10, "Counting sheeps");
    for (int i = 0; i < 10; i++)
    {
        pp.Position = i;
        Thread.Sleep(100); 
    }
    pp.Start(3); //Dreams
    pp.NextTask("Dream 1: Donuts");
    pp.NextTask("Dream 2: Beeer...");
    pp.NextTask("Dream 3: Nuclear Plant failure!!!");
    pp.Reset(); 
}

(...)

//Called from a Console Application
ProgressProxy pp = new ProgressProxy();
pp.Changed += (sender, pa) =>
{
    if ((pa.Action & ProgressAction.Task) != 0)
        Console.WriteLine("{0}", pp.CurrentTask);

    if (pp.Position != -1 )
    {
        int progress = (10 * (pp.Position - pp.Min)) / (pp.Max - pp.Min);
        string str = ".".Replicate(progress).PadRight(10);
        Console.WriteLine("[{0}] ", str);
    }
};

Sleep(pp); 
//Writes: 
//Getting Into bed
//Counting sheep
//[          ]
//[          ]
//[..        ]
//[....      ]
//[......    ]
//[........  ]
//
//[          ]
//Dream 1: Donuts
//[...       ]
//Dream 2: Beeer...
//[......    ]
//Dream 3: Nuclear Plant failure!!!
//[..........]
//
//[          ]
//

```