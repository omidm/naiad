
using System;
using System.Linq;
using System.Threading;
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
using SampleBatch = System.Collections.Generic.List<System.Collections.Generic.List<double>>;
using Weight = System.Collections.Generic.List<double>;


public class LogisticRegression
{
  // application properties.
  public Int32 dimension_;
  public Int32 iteration_num_;
  public Int32 partition_num_;
  public double sample_num_m_;
  public Int64 spin_wait_;

  // naiad properties
  public Int32 procid_;
  public Int32 worker_num_;
  public Int32 thread_num_;

  // secondary helper properties
  public Int64 pnpw_; // partition_num_per_worker_
  public Int64 snpp_; // sample_num_per_partition_;

  // properties for implementing the logic of application
  public Weight weight_;

  // multi-threaded worker needs synchronization
  private Object lock_ = new Object();

  // properties for monitoring and profiling the progress.
  public int loop_index_;
  public int loop_marker_;

  public long loop_time_stamp_;
  public List<double> total_times_;
  public List<double> compute_times_;
  public List<double> gradient_times_;

  public static int truncate_index_ = 5;

  public List<int> sample_counter;
  public List<int> sync_l1_counter_;
  public List<int> sync_l2_counter_;
  public List<int> reduce_l1_counter_;
  public List<int> reduce_l2_counter_;

  public HashSet<int> sync_tags_;
  public HashSet<int> reduce_tags_;
  public HashSet<int> gradient_tags_;

  LogisticRegression (Int32 dimension,
                      Int32 iteration_num,
                      Int32 partition_num,
                      double sample_num_m,
                      Int64 spin_wait,
                      Int32 procid,
                      Int32 worker_num,
                      Int32 thread_num) {
    dimension_ = dimension;
    iteration_num_ = iteration_num;
    partition_num_ = partition_num;
    sample_num_m_ = sample_num_m;
    spin_wait_ = spin_wait;
    procid_ = procid;
    worker_num_ = worker_num;
    thread_num_ = thread_num;

    if (partition_num_ % (worker_num_ * thread_num) != 0) {
      throw new Exception("partition number is not divisible by core number!");
    }
    if ((Int64)(sample_num_m_ * 1e6) % (Int64)(partition_num_) != 0) {
      throw new Exception("sample number is not divisible by partition number!");
    }
    pnpw_ = partition_num_ / worker_num_;
    snpp_ = (Int64)(sample_num_m_ * 1e6) / partition_num_;

    weight_ = new Weight();
    for (int j = 0; j < dimension; j++) {
      weight_.Add(1);
    }

    loop_index_ = 0;
    loop_marker_ = 0;

    total_times_ = new List<double>();
    compute_times_ = new List<double>();
    gradient_times_ = new List<double>();
    loop_time_stamp_ = Stopwatch.GetTimestamp();

    sample_counter = new List<int>();
    sync_l1_counter_ = new List<int>();
    sync_l2_counter_ = new List<int>();
    reduce_l1_counter_ = new List<int>();
    reduce_l2_counter_ = new List<int>();

    sync_tags_ = new HashSet<int>();
    reduce_tags_ = new HashSet<int>();
    gradient_tags_ = new HashSet<int>();
  }

  static void PrintHelp() {
    string str = "\nUsage:";
    str += "\n  LogisticRegression.exe <dimension>";
    str += "\n                         <iteration_num>";
    str += "\n                         <partition_num>";
    str += "\n                         <sample_num in million>";
    str += "\n                         <spin_wait in micro seconds>";
    str += "\n\nNotes:";
    str += "\n  if spin_wait is not zero the gradient phase is replaced by an exact busy loop.";
    str += "\n  naiad -n option is the number of workers.";
    str += "\n  naiad -t option is better to be the number of cores at worker.";
    str += "\n  naiad -p option should be intergers from 0 to (<worker_num> - 1).";
    str += "\n  use --inlineserializer option in the distributed mode.";
    str += "\n\nExample of running two local nodes:";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 0 -t 2 --inlineserializer 10 15 4 1 0";
    str += "\n   mono LogisticRegression.exe -n 2 --local -p 1 -t 2 --inlineserializer 10 15 4 1 0";
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
      }

      Int32 procid     = computation.Configuration.ProcessID;
      Int32 thread_num = computation.Configuration.WorkerCount;
      Int32 worker_num = computation.Configuration.Processes;

      Int32 dimension     = Int32.Parse(args[0]);
      Int32 iteration_num = Int32.Parse(args[1]);
      Int32 partition_num = Int32.Parse(args[2]);
      double sample_num_m = Convert.ToDouble(args[3]);
      Int64 spin_wait     = Int64.Parse(args[4]);

      Console.Out.WriteLine("dimension: " + dimension);
      Console.Out.WriteLine("iteration_num: " + iteration_num);
      Console.Out.WriteLine("partition_num: " + partition_num);
      Console.Out.WriteLine("sample_num_m: " + sample_num_m);
      Console.Out.WriteLine("spin_wait: " + spin_wait);
      Console.Out.WriteLine("procid: " + procid);
      Console.Out.WriteLine("worker_num: " + worker_num);
      Console.Out.WriteLine("thread_num: " + thread_num);
      Console.Out.Flush();

      LogisticRegression lr =
        new LogisticRegression(dimension,
                               iteration_num,
                               partition_num,
                               sample_num_m,
                               spin_wait,
                               procid,
                               worker_num,
                               thread_num);

      Stream<SampleBatch, Epoch> samples = lr.GenerateSamples().AsNaiadStream(computation);
      samples = samples.PartitionBy(s => (int)(s[0][0]));
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


      double average_total = lr.total_times_.GetRange(truncate_index_, iteration_num - truncate_index_).Average();
      double average_compute = lr.compute_times_.GetRange(truncate_index_, iteration_num - truncate_index_).Average();
      double average_idle = average_total - average_compute;
      Console.Out.WriteLine("*** Average for the last {0:D2} iterations: compute(ms): {1:F2} total(ms): {2:F2} (idle(ms): {3:F2})",
          iteration_num - truncate_index_, 1000 * average_compute, 1000 * average_total, 1000 * average_idle);

      Console.Out.WriteLine("Final Weight: " + PrintList(lr.weight_));
      Console.Out.WriteLine("Samples Counts: " +  PrintList(lr.sample_counter));
      Console.Out.WriteLine("Reduce Level 1 Counts: " + PrintList(lr.reduce_l1_counter_));
      Console.Out.WriteLine("Reduce Level 2 Counts: " + PrintList(lr.reduce_l2_counter_));
      Console.Out.WriteLine("Sync Level 1 Counts: " +  PrintList(lr.sync_l1_counter_));
      Console.Out.WriteLine("Sync Level 2 Counts: " +  PrintList(lr.sync_l2_counter_));
      Console.Out.WriteLine("Sync Tags: " + PrintHashSet(lr.sync_tags_));
      Console.Out.WriteLine("Reduce Tags: " + PrintHashSet(lr.reduce_tags_));
      Console.Out.WriteLine("Gradient Tags: " + PrintHashSet(lr.gradient_tags_));
      Console.Out.Flush();
    }
  }

  public Stream<SampleBatch, IterationIn<Epoch>> Advance(Stream<SampleBatch, IterationIn<Epoch>> samples)
  {
    Console.Out.WriteLine("**Begin Advance**");
    Console.Out.Flush();

    var per_core_reduced =
      samples.GroupBy(s => (int)(s[0][0]), (k, list) => Gradient(k, list));

    var per_worker_reduced =
      per_core_reduced.GroupBy(s => (int)(s[0]), (k, list) => ReductionLevel1(k, list));

    var global_reduced =
      per_worker_reduced.GroupBy(s => 0, (k, list) => ReductionLevel2(k, list));

    var per_worker_synced =
      global_reduced.GroupBy(s => (int)(s[0]), (k, list) => SyncLevel1(k, list));

    var next_samples =
      per_worker_synced.CoGroupBy(samples,
                               s => (int)(s[0]),
                               s => (int)(s[0][0]),
                               (k, weights, new_samples) => SyncLevel2(k, weights, new_samples));
    Console.Out.WriteLine("**End Advance**");
    Console.Out.Flush();

    return next_samples;
  }

  private List<Weight> Gradient(int k, IEnumerable<SampleBatch> list) {
    var start_time = Stopwatch.GetTimestamp();
    var w = new Weight();
    lock(lock_) {
      w = weight_;
      gradient_tags_.Add(k);
    }

    int sample_count = 0;
    Weight reduced = new Weight(new double[dimension_]);

    if (spin_wait_ == 0) {
      foreach (var sample_batch in list) {
        foreach (var s in sample_batch) {
          var scale = (1 / (1 + Math.Exp(s[1] * VectorDot(s, 2, w, 0))) - 1) * s[1];
          VectorAccWithScale(ref reduced, 0, s, 2, scale);
          sample_count++;
        }
      }
    } else {
      bool first_loop = true;
      var stamp = Stopwatch.GetTimestamp();
      while(true) {
        Thread.SpinWait(1000);
        var elapsed = ((double)(Stopwatch.GetTimestamp() - stamp) / (double)(Stopwatch.Frequency)) * 1e6;
        if (elapsed > spin_wait_) {
          if (first_loop) {
            Console.Out.WriteLine("spin_wait expired even before first loop!");
            Console.Out.Flush();
          }
          break;
        }
        first_loop = false;
      }
    }

    lock(lock_) {
      sample_counter.Add(sample_count);
      gradient_times_.Add((double)(Stopwatch.GetTimestamp() - start_time) / (double)(Stopwatch.Frequency));
    }

    var out_list = new List<Weight>();
    {
      var meta_reduced = new Weight();
      // set the tag for the first level reduce
      meta_reduced.Add(procid_ * thread_num_);
      meta_reduced.AddRange(reduced);
      out_list.Add(meta_reduced);
    }
    return out_list;
  }

  private List<Weight> ReductionLevel1(int k, IEnumerable<Weight> list) {
    int reduce_count = 0;
    Weight reduced = new Weight(new double[dimension_]);
    foreach (var weight in list) {
      VectorAccWithScale(ref reduced, 0, weight, 1, 1);
      reduce_count++;
    }

    lock(lock_) {
      reduce_tags_.Add(k);
      reduce_l1_counter_.Add(reduce_count);
    }

    var out_list = new List<Weight>();
    {
      out_list.Add(reduced);
    }
    return out_list;
  }


  private List<Weight> ReductionLevel2(int k, IEnumerable<Weight> list) {
    int reduce_count = 0;
    Weight reduced = new Weight(new double[dimension_]);
    foreach (var weight in list) {
      VectorAccWithScale(ref reduced, 0, weight, 0, 1);
      reduce_count++;
    }

    lock(lock_) {
      reduce_l2_counter_.Add(reduce_count);
    }

    var out_list = new List<Weight>();
    for (int i = 0; i < worker_num_; i++) {
      var extended_reduced = new Weight();
      // set the tag for the first level sync
      extended_reduced.Add(i * thread_num_);
      extended_reduced.AddRange(reduced);
      out_list.Add(extended_reduced);
    }
    return out_list;
  }


  private IEnumerable<Weight> SyncLevel1(int k, IEnumerable<Weight> list) {
    int sync_count = 0;
    var reduced = new Weight();
    foreach (var w in list) {
      reduced = w;
      sync_count++;
    }

    lock(lock_) {
      sync_l1_counter_.Add(sync_count);
    }

    var out_list = new List<Weight>();
    for (int i = 0; i < pnpw_; i++) {
      var extended_reduced = new Weight();
      var partition_id = worker_num_ * thread_num_ * (i / thread_num_) + procid_ * thread_num_ + (i % thread_num_);
      // set the tag for the second level sync
      extended_reduced.Add(partition_id);
      extended_reduced.AddRange(reduced);
      out_list.Add(extended_reduced);
    }
    return out_list;
  }


  private IEnumerable<SampleBatch> SyncLevel2(int k, IEnumerable<Weight> weights, IEnumerable<SampleBatch> samples) {
    int sync_count = 0;
    lock(lock_) {
      foreach (var w in weights) {
        for (int i = 0; i < dimension_; ++i) {
          weight_[i] = w[i+2];
        }
        sync_count++;
      }
    }

    lock(lock_) {
      sync_tags_.Add(k);
      sync_l2_counter_.Add(sync_count);
    }

    lock(lock_) {
      if (++loop_marker_ == pnpw_) {
        loop_index_++;
        loop_marker_ = 0;

        var elapsed = (double)(Stopwatch.GetTimestamp() - loop_time_stamp_) / (double)(Stopwatch.Frequency);
        loop_time_stamp_ = Stopwatch.GetTimestamp();
        total_times_.Add(elapsed);

        double gradient_max = gradient_times_.Max();
        double gradient_average = gradient_times_.Average();
        double compute = gradient_average * (pnpw_ / thread_num_);
        compute_times_.Add(compute);
        gradient_times_.Clear();

        Console.Out.WriteLine("Loop {0:D2} compute(ms): {1:F2} [gradients avg: {2:F2}, max: {3:F2}] total(ms): {4:F2}",
                              loop_index_, 1000 * compute, 1000 * gradient_average, 1000 * gradient_max, 1000 * elapsed);
        Console.Out.Flush();
      }
    }

    return samples;
  }


  public IEnumerable<SampleBatch> GenerateSamples() {
    var random = new Random(procid_);
    for (int i = 0; i < pnpw_; i++) {
      SampleBatch sample_batch = new SampleBatch();
      var partition_id = worker_num_ * thread_num_ * (i / thread_num_) + procid_ * thread_num_ + (i % thread_num_);
      for (int j = 0; j < snpp_; j++) {
        Sample sample = new Sample();
        // the first element specifies the partition id.
        sample.Add(partition_id);
        // the second element specifies the label +1/-1
        sample.Add((2 * (j % 2)) - 1);

        for (int k = 0; k < dimension_; k++) {
          sample.Add(random.Next(133));
        }
        sample_batch.Add(sample);
      }
      yield return sample_batch;
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
      return "[]";
    }
    string str = "[";
    for (int i = 0; i < length - 1; i++) {
      str += list[i];
      str += ", ";
    }
    str += list[length - 1] + "]";
    return str;
  }

  public static string PrintHashSet<T>(HashSet<T> set) {
    int length = set.Count;
    if (length == 0) {
      return "{}";
    }
    string str = "{";
    foreach (T i in set) {
      str += i;
      str += ", ";
    }
    str += "}";
    return str;
  }
}



