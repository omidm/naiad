
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
  public double sample_num_m_;
  // workers could be multiple threaded handling more than one core.
  public Int64 worker_num_;
  public Int64 snpw_; // sample_num_per_worker_;
  public Int64 pnpw_; // partition_num_per_worker_
  public Weight weight_;

  // multi-threaded worker need synchronization
  private Object lock_ = new Object();

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
                      double sample_num_m,
                      Int64 worker_num) {
    procid_ = procid;
    dimension_ = dimension;
    iteration_num_ = iteration_num;
    partition_num_ = partition_num;
    sample_num_m_ = sample_num_m;
    worker_num_ = worker_num;

    if (partition_num_ % worker_num_ != 0) {
      throw new Exception("partition number is not divisible by worker number!");
    }
    if ((Int64)(sample_num_m_ * 1e6) % partition_num_ != 0) {
      throw new Exception("sample number is not divisible by partition number!");
    }
    snpw_ = (Int64)(sample_num_m_ * 1e6) / worker_num_;
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
    str += "\n  naiad -t option is better to be the number of cores at worker";
    str += "\n  use --inlineserializer option in the distributed mode";
    str += "\n\nExample of running two local nodes:";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 0 -t 2 --inlineserializer 10 15 4 1 2";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 1 -t 2 --inlineserializer 10 15 4 1 2";
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
      double sample_num_m = Convert.ToDouble(args[3]);
      Int64 worker_num = Int64.Parse(args[4]);

      Console.Out.WriteLine("procid: " + procid);
      Console.Out.WriteLine("dimension: " + dimension);
      Console.Out.WriteLine("iteration_num: " + iteration_num);
      Console.Out.WriteLine("partition_num: " + partition_num);
      Console.Out.WriteLine("sample_num_m: " + sample_num_m);
      Console.Out.WriteLine("worker_num: " + worker_num);
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

      // var output = end_samples.Subscribe(x => {
      //                                           Console.Out.WriteLine("Final weight: " + PrintList(lr.weight_));
      //                                           Console.Out.Flush();
      //                                        });

      Console.Out.WriteLine("Before Activate!");
      Console.Out.Flush();
  
      // start the computation, fixing the structure of the dataflow graph.
      computation.Activate();

      Console.Out.WriteLine("After Activate!");
      Console.Out.Flush();
  
      // block until all work is finished.
      computation.Join();

      Console.Out.WriteLine("After Join!");
      Console.Out.WriteLine("Final weight: " + PrintList(lr.weight_));
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


    var local_reduced =
      samples.GroupBy(s => (int)(s[0]), (k, list) => Gradient(k, list));

    var global_reduced =
      local_reduced.GroupBy(s => 0, (k, list) => Reduction(k, list));

    var next_samples =
      global_reduced.CoGroupBy(samples,
                               s => (int)(s[0]),
                               s => (int)(s[0]),
                               (k, weights, new_samples) => Synch(k, weights, new_samples));

    Console.Out.WriteLine("**End Advance**");
    Console.Out.Flush();

    return next_samples;
  }

  private List<Sample> Gradient(int k, IEnumerable<Sample> list) {
    var w = new Weight();
    lock(lock_) {
      w = weight_;
    }

    int count = 0;
    Weight reduced = new Weight(new double[dimension_]);
    foreach (var s in list) {
      var scale = (1 / (1 + Math.Exp(s[1] * VectorDot(s, 2, w, 0))) - 1) * s[1];
      VectorAccWithScale(ref reduced, 0, s, 2, scale);
      count++;
    }

    lock(lock_) {
      counter_1_.Add(count);
    }

    var out_list = new List<Sample>();
    out_list.Add(reduced);
    return out_list;
  }

  private List<Sample> Reduction(int k, IEnumerable<Sample> list) {
    int count = 0;
    Weight reduced = new Weight(new double[dimension_]);
    foreach (var weight in list) {
      VectorAccWithScale(ref reduced, 0, weight, 0, 1);
      count++;
    }

    lock(lock_) {
      counter_2_.Add(count);
      loop_index_++;
      long temp = stopwatch_.ElapsedMilliseconds;
      long diff = temp - elapsed_milli_;
      elapsed_milli_ = temp;
      Console.Out.WriteLine("Loop " + loop_index_ + " elapsed(ms): " + diff);
      Console.Out.Flush();
    }

    var out_list = new List<Sample>();
    for (int i = 0; i < partition_num_; ++i) {
      var w = new Weight();
      w.Add(i);
      w.AddRange(reduced);
      out_list.Add(w);
    }
    return out_list;

  }

  private IEnumerable<Sample> Synch(int k, IEnumerable<Weight> weights, IEnumerable<Sample> samples) {
    int count = 0;
    foreach (var w in weights) {
      lock(lock_) {
        for (int i = 0; i < dimension_; ++i) {
          weight_[i] = w[i+1];
        }
      }
      count++;
    }
    lock(lock_) {
      counter_3_.Add(count);
    }
    return samples;
  }

  public IEnumerable<Sample> GenerateSamples() {
    var random = new Random(procid_);
    for (int i = 0; i < snpw_; i++) {
      Sample sample = new Sample();
      // the first element specifies the partition id.
      sample.Add(pnpw_ * procid_ + (i % pnpw_));

      // the second element specifies the label +1/-1
      sample.Add((2 * (i % 2)) - 1);

      for (int j = 0; j < dimension_; j++) {
        sample.Add(random.Next(133));
      }

      yield return sample;
    }
  }

  public static double VectorDot(List<double> v1, int of1, List<double>v2, int of2) {
    Debug.Assert((v1.Count - of1) == (v2.Count - of2));
    double result = 0;
    for (int i = 0; i < (v1.Count - of1); ++i) {
      result += v1[i+of1] * v2[i+of2];
    }
    return result;
  }

  public static void VectorAccWithScale(ref List<double> v1, int of1,
                                        List<double> v2, int of2,
                                        double scale) {
    Debug.Assert((v1.Count - of1) == (v2.Count - of2));
    for (int i = 0; i < (v1.Count - of1); ++i) {
      v1[i+of1] = v1[i+of1] + scale * v2[i+of2];
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


