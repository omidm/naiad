
using System;
using Microsoft.Research.Naiad;
using Microsoft.Research.Naiad.Input;

public class FirstNaiadProgram
{
  static void Main(string[] args)
  {
    // 1. allocate a new dataflow computation.
    using (var computation = NewComputation.FromArgs(ref args))
    {
      Console.Out.WriteLine("Proc ID: " + computation.Configuration.ProcessID);
      Console.Out.Flush();

      // 2. define an object which accepts input strings.
      var source = new BatchedDataSource<string>();
  
      // 3. convert the data source into a Naiad stream of strings.
      var input = computation.NewInput(source);
  
      // 4.request a notification for each batch of strings
      // received.
      var output = input.Subscribe(x =>
          {
          foreach (var line
            in x)
          Console.WriteLine(line);
          });
  
      Console.Out.WriteLine("Before Activate!");
      Console.Out.Flush();
  
      // 5. start the computation, fixing the structure of
      // the dataflow graph.
      computation.Activate();
  
      Console.Out.WriteLine("After Activate!");
      Console.Out.Flush();
  
      // 6. read inputs from the console as long as the
      // user supplies them.
      for (var l = Console.ReadLine(); l.Length > 0; l
          = Console.ReadLine())
        source.OnNext(l.Split());
  
      // 7. signal that the source is now complete.
      source.OnCompleted();
  
      // 8. block until all work is finished.
      computation.Join();
    }
  }
}

