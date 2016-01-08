
using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Research.Naiad;
using Microsoft.Research.Naiad.Input;
using Microsoft.Research.Naiad.Frameworks.Lindi;
using Microsoft.Research.Naiad.Frameworks.GraphLINQ;
using Microsoft.Research.Naiad.Dataflow;
using Microsoft.Research.Naiad.Dataflow.Iteration;
using Microsoft.Research.Naiad.Dataflow.PartitionBy;

using Sample = System.Collections.Generic.List<double>;
using Weight = System.Collections.Generic.List<double>;


public class LogisticRegression
{
  // properties for implementing the logic of the application.
  public int procid_;
  public int dimension_;
  public int iteration_num_;
  public Int64 partition_num_;
  public Int64 sample_num_m_;
  // each worker is single threaded, so core_num == worker_num
  // there "might" be a way to leverage the thread number.
  public Int64 worker_num_;
  public Int64 snpw_; // sample_num_per_worker_;
  public Int64 pnpw_; // partition_num_per_worker_
  public Weight weight_;

  // properties for monitoring and profiling the progress.
  public int loop_index_;
  public long elapsed_milli_;
  public List<int> counter_1_;
  public List<int> counter_2_;
  public List<int> counter_3_;
  public System.Diagnostics.Stopwatch stopwatch_;

  LogisticRegression (int procid,
                      int dimension,
                      int iteration_num,
                      int partition_num,
                      int sample_num_m,
                      int worker_num) {
    procid_ = procid;
    dimension_ = dimension;
    iteration_num_ = iteration_num;
    partition_num_ = partition_num;
    sample_num_m_ = sample_num_m;
    worker_num_ = worker_num;

    Debug.Assert(partition_num_ % worker_num_ == 0);
    Debug.Assert((sample_num_m_ * (Int64)1e6) % partition_num_ == 0);
    snpw_ = sample_num_m_ * (Int64)1e6 / worker_num_;
    pnpw_ = partition_num_ / worker_num_;

    weight_ = new Weight();
    for (int j = 0; j < dimension; j++) {
      weight_.Add(1);
    }

    loop_index_ = 0;
    elapsed_milli_ = 0;
    counter_1_ = new List<int>();
    counter_2_ = new List<int>();
    counter_3_ = new List<int>();
    stopwatch_ = new System.Diagnostics.Stopwatch();
    stopwatch_.Start();
  }

  static void PrintHelp() {
    string str = "Usage:";
    str += "\n  LogisticRegression.exe <dimension> <iteration_num> <partition_num> <sample_num in million> <worker_num>";
    str += "\nNote: naiad -n option should be worker_num";
    str += "\nNote: naiad -p option should be intergers from 0 to worker_num - 1";
    str += "\nNote: use --inlineserializer option in the distributed mode";
    str += "\nExample running two local nodes:";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 0 --inlineserializer 10 15 4 1 2";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 1 --inlineserializer 10 15 4 1 2";
    Console.Out.WriteLine(str);
    Console.Out.Flush();
  }

  static void Main(string[] args)
  {
    // 1. allocate a new dataflow computation.
    using (var computation = NewComputation.FromArgs(ref args))
    {

      if (args.Length != 5) {
        // PrintHelp();
        // return;
      }

      Int32 iterations = 10;
      if (args.Length >= 1) {
        iterations = Int32.Parse(args[0]);
      }
      Console.Out.WriteLine("iterations: " + iterations);
      Console.Out.Flush();

      Int32 counts = 10;
      if (args.Length >= 2) {
        counts = Int32.Parse(args[1]);
      }
      Console.Out.WriteLine("counts: " + counts);
      Console.Out.Flush();

      int dimension = 10;


      Stream<Sample, Epoch> nodes =
        GenerateSamples(dimension, counts, computation.Configuration.ProcessID).AsNaiadStream(computation);

      nodes = nodes.PartitionBy(x => (int)(x[x.Count -1]));

      // Stream<Weight, Epoch> weight =
      //   GenerateWeight(dimension, computation.Configuration.ProcessID).AsNaiadStream(computation);
      // nodes = nodes.PartitionBy(x => x.source);

      // nodes.IterateAndAccumulate((lc, x)  => x, x => Print(x), iterations, "LogisticRegression");


      LogisticRegression lg = new LogisticRegression(computation.Configuration.ProcessID,
                                                     dimension,
                                                     1,
                                                     1,
                                                     1,
                                                     1);

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
      Console.Out.WriteLine("Counter 1: " + lg.procid_ +  PrintList(lg.counter_1_));
      Console.Out.WriteLine("Counter 2: " + lg.procid_ +  PrintList(lg.counter_2_));
      Console.Out.WriteLine("Counter 3: " + lg.procid_ +  PrintList(lg.counter_3_));
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

    var fx = x.Select(s => {  for (int i = 0; i < s.Count - 1; i++) {
                                s[i] =  1 * s[i];
                              }
                              // Console.Out.WriteLine("SELECT");
                              Console.Out.Flush();
                              return s;
                           });

    var local_reduced = fx.GroupBy(s => (int)(s[s.Count - 1]), (k, list) => {
                                                               loop_index_++;
                                                               long temp = stopwatch_.ElapsedMilliseconds;
                                                               long diff = temp - elapsed_milli_;
                                                               elapsed_milli_ = temp;
                                                               Console.Out.WriteLine("REDUCE " + loop_index_ + " elapsed(ms): " + diff);
                                                               Console.Out.Flush();
                                                               int c = 0;
                                                               var out_list = new List<Sample>();
                                                               foreach (var sample in list) {
                                                                if (c == 0) {
                                                                  out_list.Add(sample);
                                                                }
                                                                c++;
                                                               }
                                                               counter_1_.Add(c);
                                                               return out_list;
                                                             });

    var global_reduced = local_reduced.GroupBy(i => 0, (k, list) => {
                                                               int c = 0;
                                                               var out_list = new List<Sample>();
                                                               foreach (var sample in list) {
                                                                
                                                                 var sample2 = new Sample(sample);
                                                                 sample.Add(2 * c);
                                                                 sample2.Add(2 * c + 1);
                                                                 out_list.Add(sample);
                                                                 out_list.Add(sample2);
                                                                 //sample.Add(c);
                                                                 // out_list.Add(sample);

                                                                 c++;
                                                               }
                                                               counter_2_.Add(c);
                                                               return out_list;
                                                             });
    // global_reduced = global_reduced.PartitionBy(s => (int)s[s.Count - 1]);

    var bottle_neck = global_reduced.CoGroupBy(y, s => {var id =  s[s.Count - 1]; /* Console.Out.WriteLine("id1: " + id); Console.Out.Flush();*/ return (int)id;},
                                                  s => {var id =  s[s.Count - 1]; /* Console.Out.WriteLine("id2: " + id); Console.Out.Flush();*/ return (int)id;},
                                                  (k, l1, l2) => {
                                                               int c = 0;
                                                               foreach (var sample in l1) {
                                                                c++;
                                                               }
                                                               counter_3_.Add(c);
                                                               return l2;
                                                             });

    // bottle_neck = bottle_neck.PartitionBy(s => (int)s[s.Count - 1]);
    // var next_wave = Microsoft.Research.Naiad.Frameworks.Lindi.ExtensionMethods.Concat(y, bottle_neck);
 


    Console.Out.WriteLine("**Operate After**");
    Console.Out.Flush();

    return bottle_neck;
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

      // to differentiate the pid!
      sample.Add(2 * seed + (i % 2));
      // sample.Add(seed);

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

  public static string PrintList<T>(List<T> list) {
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

