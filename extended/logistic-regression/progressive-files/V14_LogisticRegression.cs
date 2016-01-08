
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
  public Int32 procid_;
  public Int32 dimension_;
  public Int32 iteration_num_;
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

  LogisticRegression (Int32 procid,
                      Int32 dimension,
                      Int32 iteration_num,
                      Int64 partition_num,
                      Int64 sample_num_m,
                      Int64 worker_num) {
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
    string str = "\nUsage:";
    str += "\n  LogisticRegression.exe <dimension> <iteration_num> <partition_num> <sample_num in million> <worker_num>";
    str += "\n\nNotes:";
    str += "\n  naiad -n option should be worker_num";
    str += "\n  naiad -p option should be intergers from 0 to (worker_num-1)";
    str += "\n  use --inlineserializer option in the distributed mode";
    str += "\n\nExample of running two local nodes:";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 0 --inlineserializer 10 15 4 1 2";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 1 --inlineserializer 10 15 4 1 2";
    str += "\n";
    Console.Out.WriteLine(str);
    Console.Out.Flush();
  }

  static void Main(string[] args)
  {
    // 1. allocate a new dataflow computation.
    using (var computation = NewComputation.FromArgs(ref args))
    {

      if (args.Length != 5) {
        PrintHelp();
        return;
      } else if (args.Length == 1) {
        if (args[0] == "--help" || args[0] == "-h") {
          PrintHelp();
          return;
        }
      }


      Int32 procid = computation.Configuration.ProcessID;
      Int32 dimension = Int32.Parse(args[0]);
      Int32 iteration_num = Int32.Parse(args[1]);
      Int64 partition_num = Int32.Parse(args[2]);
      Int64 sample_num_m = Int64.Parse(args[3]);
      Int64 worker_num = Int64.Parse(args[4]);

      Console.Out.WriteLine("**NOTE: Worker num should be equal to core num!");
      Console.Out.WriteLine("procid: " + procid);
      Console.Out.WriteLine("dimension: " + dimension);
      Console.Out.WriteLine("iteration_num: " + iteration_num);
      Console.Out.WriteLine("partition_num: " + partition_num);
      Console.Out.WriteLine("sample_num_m: " + sample_num_m);
      Console.Out.WriteLine("worker_num (should be core num): " + worker_num);
      Console.Out.Flush();

      LogisticRegression lr =
        new LogisticRegression(procid,
                               dimension,
                               iteration_num,
                               partition_num,
                               sample_num_m,
                               worker_num);

      Stream<Sample, Epoch> samples = lr.GenerateSamples().AsNaiadStream(computation);

      // partition the samples based on the first element.
      samples = samples.PartitionBy(s => (int)(s[0]));


      var end_samples  = samples.Iterate((lc , s) => lr.Advance(s), iteration_num, "LogisticRegression");

      var output = end_samples.Subscribe(x => {
                                                Console.Out.WriteLine("Final weight: " + PrintList(lr.weight_));
                                                Console.Out.Flush();
                                             });

      Console.Out.WriteLine("Before Activate!");
      Console.Out.Flush();
  
      // start the computation, fixing the structure of the dataflow graph.
      computation.Activate();

      Console.Out.WriteLine("After Activate!");
      Console.Out.Flush();
  
      // block until all work is finished.
      computation.Join();

      Console.Out.WriteLine("After Join!");
      Console.Out.WriteLine("Counter 1 from procid: " + lr.procid_ + " " +  PrintList(lr.counter_1_));
      Console.Out.WriteLine("Counter 2 from procid: " + lr.procid_ + " " +  PrintList(lr.counter_2_));
      Console.Out.WriteLine("Counter 3 from procid: " + lr.procid_ + " " +  PrintList(lr.counter_3_));
      Console.Out.Flush();
    }
  }


  public Stream<Sample, IterationIn<Epoch>> Advance(Stream<Sample, IterationIn<Epoch>> samples)
  {
    Console.Out.WriteLine("**Begin Advance**");
    Console.Out.Flush();

    var next_samples = samples;

    var scaled_samples =
      samples.Select(s => {
                            for (int i = 1; i < s.Count; i++) {
                              // OMID: do the computation
                              s[i] =  1 * s[i];
                            }
                            // Console.Out.WriteLine("SELECT");
                            // Console.Out.Flush();
                            return s;
                          });

    var local_reduced =
      scaled_samples.GroupBy(s => (int)(s[0]),
                             (k, list) => {
                                            loop_index_++;
                                            long temp = stopwatch_.ElapsedMilliseconds;
                                            long diff = temp - elapsed_milli_;
                                            elapsed_milli_ = temp;
                                            Console.Out.WriteLine("Loop " + loop_index_ + " elapsed(ms): " + diff);
                                            Console.Out.Flush();

                                            int c = 0;
                                            var out_list = new List<Sample>();
                                            Sample reduced = new Sample(new double[dimension_]);
                                            foreach (var sample in list) {
                                              // OMID: do computation
                                              c++;
                                            }
                                            out_list.Add(reduced);
                                            counter_1_.Add(c);
                                            return out_list;
                                          });

    var global_reduced =
      local_reduced.GroupBy(s => 0,
                            (k, list) => {
                                           int c = 0;
                                           var out_list = new List<Sample>();
                                           var reduce = new Sample();
                                           foreach (var sample in list) {
                                             // OMID: do computation
                                             c++;
                                           }
                                           counter_2_.Add(c);

                                           for (int i = 0; i < partition_num_; ++i) {
                                             var w = new Sample();
                                             w.Add(i);
                                             w.AddRange(reduce);
                                             out_list.Add(w);
                                           }
                                           return out_list;
                                         });

    var bottle_neck =
      global_reduced.CoGroupBy(next_samples,
                               s => (int)(s[0]),
                               s => (int)(s[0]),
                               (k, weights, nsamples) => {
                                                int c = 0;
                                                foreach (var w in weights) {
                                                  // OMID: do computation
                                                  c++;
                                                }
                                                counter_3_.Add(c);
                                                return nsamples;
                                              });

    Console.Out.WriteLine("**End Advance**");
    Console.Out.Flush();

    return bottle_neck;
  }


  public IEnumerable<Sample> GenerateSamples() {
    var random = new Random(procid_);
    for (int i = 0; i < snpw_; i++) {
      Sample sample = new Sample();
      // the first element specifies the partition id.
      sample.Add(pnpw_ * procid_ + (i % pnpw_));

      for (int j = 0; j < dimension_; j++) {
        sample.Add(random.Next(133));
      }

      yield return sample;
    }
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

