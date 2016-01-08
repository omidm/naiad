using System;
using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// 
using Microsoft.Research.Naiad;
using Microsoft.Research.Naiad.Input;
using Microsoft.Research.Naiad.Frameworks.Lindi;
using Microsoft.Research.Naiad.Frameworks.GraphLINQ;
// 
using Microsoft.Research.Naiad.Dataflow;
using Microsoft.Research.Naiad.Dataflow.Iteration;
using Microsoft.Research.Naiad.Dataflow.PartitionBy;



public class LogisticRegression
{
  static void Main(string[] args)
  {
    // 1. allocate a new dataflow computation.
    using (var computation = NewComputation.FromArgs(ref args))
    {

      Int32 iterations = 30;
      if (args.Length >= 1) {
        iterations = Int32.Parse(args[0]);
      }

      Console.Out.WriteLine("iterations: " + iterations);
      Console.Out.Flush();

      int counts = 5;
      Stream<Node, Epoch> nodes = GenerateNodes(counts, computation.Configuration.ProcessID).AsNaiadStream(computation);
      // nodes = nodes.PartitionBy(x => x.source);

      // nodes.IterateAndAccumulate((lc, x)  => x, x => Print(x), iterations, "LogisticRegression");

      var end_nodes  = nodes.Iterate((lc , x) => Operate(x), iterations, "LogisticRegression");
      var node_count = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Count(end_nodes);
      // var end_nodes  = nodes.IterateAndAccumulate((lc , x) => Operate(x), iterations, "LogisticRegression");

      Console.Out.WriteLine("Proc ID: " + computation.Configuration.ProcessID);
      Console.Out.Flush();

      // 2. define an object which accepts input strings.
      // var source = new BatchedDataSource<string>();
  
      // 3. convert the data source into a Naiad stream of strings.
      // var input = computation.NewInput(source);
  
      // 4.request a notification for each batch of strings
      // received.
      // var output = input.Subscribe(x =>
      //     {
      //     foreach (var line
      //       in x)
      //     Console.WriteLine(line);
      //     });
  
      Console.Out.WriteLine("Before Activate!");
      Console.Out.Flush();
  
      // 5. start the computation, fixing the structure of
      // the dataflow graph.
      computation.Activate();

      Console.Out.WriteLine("After Activate!");
      Console.Out.Flush();
  
      // 6. read inputs from the console as long as the
      // user supplies them.
      // for (var l = Console.ReadLine(); l.Length > 0; l
      //     = Console.ReadLine()) {}
      //   // source.OnNext(l.Split());
  
      // 7. signal that the source is now complete.
      // source.OnCompleted();
  
      // 8. block until all work is finished.
      computation.Join();

      Console.Out.WriteLine("After Join!");
      Console.Out.WriteLine("Node Count: " + node_count);
      Console.Out.Flush();
    }
  }


  public static int Print(int x)
  {
    Console.Out.WriteLine("Elem: " + x);
    Console.Out.Flush();
    return x;
  }

  public static Stream<Node, IterationIn<Epoch>> Operate(Stream<Node, IterationIn<Epoch>> x)
  {
    // Console.Out.WriteLine("**Operate on Context: " + x.Context);
    Console.Out.WriteLine("**Operate Before **");
    Console.Out.Flush();
    var y = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Concat(x, x);
    Console.Out.WriteLine("**Operate After**");
    return y;
  }

  public static IEnumerable<Node> GenerateNodes(int counts, int seed)
  {
    // var random = new Random(seed);
    for (int i = 0; i < counts; i++)
      // yield return new Node(random.Next(13));
      yield return new Node(seed);
  }

}

