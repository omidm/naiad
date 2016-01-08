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

using Sample = System.Collections.Generic.List<float>;
using Weight = System.Collections.Generic.List<float>;

public class LogisticRegression
{
  // public class Sample {
  //   public Sample() {
  //     vector = new List<float>();
  //     // vector = new List<float>(new float[dimension]);
  //     // label = 0;
  //   }
  //   public List<float> vector;
  //   public float label;
  // }


  public Weight weight;
  public int procid;
  public int sample_counter;
  public int reduce_index;
  public Sample reduction_counter;
  public System.Diagnostics.Stopwatch stopwatch;
  public long elapsed_milli;

  LogisticRegression (int dimension, int pid) {
    weight = new Weight();
    for (int j = 0; j < dimension; j++) {
      weight.Add(1000 + pid);
    }
    procid = pid;
    sample_counter = 0;
    reduce_index = 0;
    reduction_counter = new Sample();
    stopwatch = new System.Diagnostics.Stopwatch();
    stopwatch.Start();
    elapsed_milli = 0;
  }

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

      Int32 counts = 5;
      if (args.Length >= 2) {
        counts = Int32.Parse(args[1]);
      }
      Console.Out.WriteLine("counts: " + counts);
      Console.Out.Flush();

      int dimension = 10;


      Stream<Sample, Epoch> nodes =
        GenerateSamples(dimension, counts, computation.Configuration.ProcessID).AsNaiadStream(computation);

      nodes = nodes.PartitionBy(x => computation.Configuration.ProcessID);

      // Stream<Weight, Epoch> weight =
      //   GenerateWeight(dimension, computation.Configuration.ProcessID).AsNaiadStream(computation);
      // nodes = nodes.PartitionBy(x => x.source);

      // nodes.IterateAndAccumulate((lc, x)  => x, x => Print(x), iterations, "LogisticRegression");


      LogisticRegression lg = new LogisticRegression(dimension, computation.Configuration.ProcessID);

      var end_nodes  = nodes.Iterate((lc , x) => lg.Operate(x), iterations, "LogisticRegression");
      var node_count = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Count(end_nodes);
      end_nodes.WriteToFiles("output_nodes_{0}.txt", (record, writer) => writer.Write(true));
      node_count.WriteToFiles("output_count_{0}.txt", (record, writer) => writer.Write(true));
      // var end_nodes  = nodes.IterateAndAccumulate((lc , x) => Operate(x), iterations, "LogisticRegression");

      Console.Out.WriteLine("Proc ID: " + computation.Configuration.ProcessID);
      Console.Out.Flush();

      var output = node_count.Subscribe(x => {
                                                foreach (var e in x) {
                                                  // Console.WriteLine("vector: " + PrintList(e.First) + " count: " + e.Second);
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
      Console.Out.WriteLine("Sample Counter: " + lg.sample_counter);
      Console.Out.WriteLine("Reduction Counter: " + PrintList(lg.reduction_counter));
      Console.Out.Flush();
    }
  }


  public static int Print(int x)
  {
    Console.Out.WriteLine("Elem: " + x);
    Console.Out.Flush();
    return x;
  }

  public Stream<Sample, IterationIn<Epoch>> Operate(Stream<Sample, IterationIn<Epoch>> x)
  {
    // Console.Out.WriteLine("**Operate on Context: " + x.Context);
    Console.Out.WriteLine("**Operate Before **");
    Console.Out.Flush();
    // var y = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Concat(x, x);
    // var y = x.Select( e => new Node(2 * e));

    // var y = x.Select( s => {Sample ns = s; ns[counter] = 100 + counter++; return ns;});

    var y = x;

    var fx = x.Select(s => {  for (int i = 0; i < s.Count; i++) {
                                s[i] =  1 * s[i];
                              }
                              // Console.Out.WriteLine("SELECT");
                              Console.Out.Flush();
                              sample_counter++;
                              return s;
                           });

    var local_reduced = fx.GroupBy(i => procid, (k, list) => {
                                                               reduce_index++;
                                                               long temp = stopwatch.ElapsedMilliseconds;
                                                               long diff = temp - elapsed_milli;
                                                               elapsed_milli = temp;
                                                               Console.Out.WriteLine("REDUCE " + reduce_index + " elapsed(ms): " + diff);
                                                               Console.Out.Flush();
                                                               int c = 0;
                                                               foreach (var sample in list) {
                                                                c++;
                                                               }
                                                               reduction_counter.Add(c);
                                                               return list;
                                                             });

    Console.Out.WriteLine("**Operate After**");
    Console.Out.Flush();

    return y;
  }

  public static IEnumerable<Sample> GenerateSamples(int dimension, int counts, int seed)
  {
    var random = new Random(seed);
    for (int i = 0; i < counts; i++) {
      Sample sample = new Sample();
      // sample.Add(seed);
      // sample.Add(i % 2 + 100);
      for (int j = 0; j < dimension; j++) {
        // sample.Add(random.Next(13));
        sample.Add(seed);
        // sample.vector.Add(seed);
      }
      // Console.Out.WriteLine("Random sample: " + PrintList(sample));
      Console.Out.Flush();
      yield return sample;
      // yield return new Node(seed);
    }
  }

  public static IEnumerable<Sample> GenerateWeight(int dimension, int seed)
  {
    // var random = new Random(seed);
    Weight weight = new Weight();
    weight.Add(seed);
    for (int j = 0; j < dimension; j++) {
      // sample.Add(random.Next(13));
      weight.Add(seed);
    }
    Console.Out.WriteLine("Weight: " + PrintList(weight));
    Console.Out.Flush();
    yield return weight;
    // yield return new Node(seed);
  }


  public static string PrintList(Sample sample) {
    int length = sample.Count;
    // int length = sample.vector.Count;
    if (length == 0) {
      return "{}";
    }
    string str = "{";
    for (int i = 0; i < length - 1; i++) {
      str += sample[i];
      // str += sample.vector[i];
      str += ", ";
    }
    str += sample[length - 1] + "}";
    // str += sample.vector[length - 1] + "}";
    return str;
  }

}

