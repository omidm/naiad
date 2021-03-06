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

      Int32 iterations = 10;
      if (args.Length >= 1) {
        iterations = Int32.Parse(args[0]);
      }

      Console.Out.WriteLine("iterations: " + iterations);
      Console.Out.Flush();

      int counts = 5;
      int dimension = 10;
      Stream<List<float>, Epoch> nodes =
        GenerateNodes(dimension, counts, computation.Configuration.ProcessID).AsNaiadStream(computation);
      // nodes = nodes.PartitionBy(x => x.source);

      // nodes.IterateAndAccumulate((lc, x)  => x, x => Print(x), iterations, "LogisticRegression");

      var end_nodes  = nodes.Iterate((lc , x) => Operate(x), iterations, "LogisticRegression");
      var node_count = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Count(end_nodes);
      end_nodes.WriteToFiles("output_nodes_{0}.txt", (record, writer) => writer.Write(true));
      node_count.WriteToFiles("output_count_{0}.txt", (record, writer) => writer.Write(true));
      // var end_nodes  = nodes.IterateAndAccumulate((lc , x) => Operate(x), iterations, "LogisticRegression");

      Console.Out.WriteLine("Proc ID: " + computation.Configuration.ProcessID);
      Console.Out.Flush();

      var output = node_count.Subscribe(x => {
                                                foreach (var e in x) {
                                                  Console.WriteLine("vector: " + PrintList(e.First) + " count: " + e.Second);
                                                }
                                             });

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
      Console.Out.Flush();
    }
  }


  public static int Print(int x)
  {
    Console.Out.WriteLine("Elem: " + x);
    Console.Out.Flush();
    return x;
  }

  public static Stream<List<float>, IterationIn<Epoch>> Operate(Stream<List<float>, IterationIn<Epoch>> x)
  {
    // Console.Out.WriteLine("**Operate on Context: " + x.Context);
    Console.Out.WriteLine("**Operate Before **");
    Console.Out.Flush();
    // var y = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Concat(x, x);
    // var y = x.Select( e => new Node(2 * e));
    var y = x;

    Console.Out.WriteLine("**Operate After**");

    return y;
  }

  public static IEnumerable<List<float>> GenerateNodes(int dimension, int counts, int seed)
  {
    var random = new Random(seed);
    for (int i = 0; i < counts; i++) {
      List<float> sample = new List<float>();
      for (int j = 0; j < dimension; j++) {
        sample.Add(random.Next(13));
      }
      Console.Out.WriteLine("Random sample: " + PrintList(sample));
      Console.Out.Flush();
      yield return sample;
      // yield return new Node(seed);
    }
  }

  public static string PrintList(List<float> list) {
    int length = list.Count;
    if (length == 0) {
      return "{}";
    }
    string str = "{";
    for (int i = 0; i < length - 1; i++) {
      str += list[i];
      str += ", ";
    }
    str += list[length - 1] + "}";
    return str;
  }

}

