
using System;
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
using Mean = System.Collections.Generic.List<double>;
using Means = System.Collections.Generic.List<System.Collections.Generic.List<double>>;

public class KMeans
{
  // properties for implementing the logic of the application.
  public Int32 procid_;
  public Int32 dimension_;
  public Int32 cluster_num_;
  public Int32 iteration_num_;
  public Int64 partition_num_;
  public double sample_num_m_;
  // workers could be multiple threaded handling more than one core.
  public Int64 worker_num_;
  public Int64 snpw_; // sample_num_per_worker_;
  public Int64 pnpw_; // partition_num_per_worker_
  public Int64 spin_wait_;
  public Means means_;

  // multi-threaded worker need synchronization
  private Object lock_ = new Object();

  // properties for monitoring and profiling the progress.
  public int p_counter_;
  public int loop_index_;
  public double clustering_elapsed_;
  public long loop_stamp_;
  public long clustering_stamp_;
  public List<int> counter_1_;
  public List<int> counter_2_;
  public List<int> counter_3_;

  KMeans (Int32 procid,
          Int32 dimension,
          Int32 cluster_num,
          Int32 iteration_num,
          Int64 partition_num,
          double sample_num_m,
          Int64 worker_num,
          Int64 spin_wait) {
    procid_ = procid;
    dimension_ = dimension;
    cluster_num_ = cluster_num;
    iteration_num_ = iteration_num;
    partition_num_ = partition_num;
    sample_num_m_ = sample_num_m;
    worker_num_ = worker_num;
    spin_wait_ = spin_wait;

    if (partition_num_ % worker_num_ != 0) {
      throw new Exception("partition number is not divisible by worker number!");
    }
    if ((Int64)(sample_num_m_ * 1e6) % partition_num_ != 0) {
      throw new Exception("sample number is not divisible by partition number!");
    }
    snpw_ = (Int64)(sample_num_m_ * 1e6) / worker_num_;
    pnpw_ = partition_num_ / worker_num_;

    means_ = new Means();
    for (int i = 0; i < cluster_num; ++i) {
      Mean m = new Mean();
      for (int j = 0; j < dimension; j++) {
        m.Add(i);
      }
      means_.Add(m);
    }

    p_counter_ = 0;
    loop_index_ = 0;
    clustering_elapsed_ = 0;
    loop_stamp_ = Stopwatch.GetTimestamp();
    clustering_stamp_ = Stopwatch.GetTimestamp();
    counter_1_ = new List<int>();
    counter_2_ = new List<int>();
    counter_3_ = new List<int>();
  }

  static void PrintHelp() {
    string str = "\nUsage:";
    str += "\n  KMeans.exe <dimension>";
    str += "\n             <cluster_num>";
    str += "\n             <iteration_num>";
    str += "\n             <partition_num>";
    str += "\n             <sample_num in million>";
    str += "\n             <worker_num>";
    str += "\n             <spin_wait in micro seconds>";
    str += "\n\nNotes:";
    str += "\n  if spin_wait is not zero the clustering phase is replaced by an exact busy loop.";
    str += "\n  naiad -n option should be worker_num";
    str += "\n  naiad -p option should be intergers from 0 to (worker_num-1)";
    str += "\n  naiad -t option is better to be the number of cores at worker";
    str += "\n  use --inlineserializer option in the distributed mode";
    str += "\n\nExample of running two local nodes:";
    str += "\n   mono KMeans.exe -n 2 --local -p 0 -t 2 --inlineserializer 10 15 4 1 2 0";
    str += "\n   mono KMeans.exe -n 2 --local -p 1 -t 2 --inlineserializer 10 15 4 1 2 0";
    str += "\n";
    Console.Out.WriteLine(str);
    Console.Out.Flush();
  }

  static void Main(string[] args)
  {
    // 1. allocate a new dataflow computation.
    using (var computation = NewComputation.FromArgs(ref args))
    {

      if (args.Length != 7) {
        PrintHelp();
        return;
      }


      Int32 procid = computation.Configuration.ProcessID;
      Int32 dimension = Int32.Parse(args[0]);
      Int32 cluster_num = Int32.Parse(args[1]);
      Int32 iteration_num = Int32.Parse(args[2]);
      Int64 partition_num = Int32.Parse(args[3]);
      double sample_num_m = Convert.ToDouble(args[4]);
      Int64 worker_num = Int64.Parse(args[5]);
      Int64 spin_wait = Int64.Parse(args[6]);

      Console.Out.WriteLine("procid: " + procid);
      Console.Out.WriteLine("dimension: " + dimension);
      Console.Out.WriteLine("cluster_num: " + cluster_num);
      Console.Out.WriteLine("iteration_num: " + iteration_num);
      Console.Out.WriteLine("partition_num: " + partition_num);
      Console.Out.WriteLine("sample_num_m: " + sample_num_m);
      Console.Out.WriteLine("worker_num: " + worker_num);
      Console.Out.WriteLine("spin_wait: " + spin_wait);
      Console.Out.Flush();

      KMeans km =
        new KMeans(procid,
                   dimension,
                   cluster_num,
                   iteration_num,
                   partition_num,
                   sample_num_m,
                   worker_num,
                   spin_wait);

      Stream<Sample, Epoch> samples = km.GenerateSamples().AsNaiadStream(computation);

      // partition the samples based on the first element.
      samples = samples.PartitionBy(s => (int)(s[0]));


      var end_samples  = samples.Iterate((lc , s) => km.Advance(s), iteration_num, "KMeans");

      // var output = end_samples.Subscribe(x => {
      //                                           Console.Out.WriteLine("Final centers: " + PrintListList(km.weight_));
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
      Console.Out.WriteLine("Final centers: " + PrintListList(km.means_));
      Console.Out.WriteLine("Counter 1 from procid: " + km.procid_ + " " +  PrintList(km.counter_1_));
      Console.Out.WriteLine("Counter 2 from procid: " + km.procid_ + " " +  PrintList(km.counter_2_));
      Console.Out.WriteLine("Counter 3 from procid: " + km.procid_ + " " +  PrintList(km.counter_3_));
      Console.Out.Flush();
    }
  }

  public Stream<Sample, IterationIn<Epoch>> Advance(Stream<Sample, IterationIn<Epoch>> samples)
  {
    Console.Out.WriteLine("**Begin Advance**");
    Console.Out.Flush();


    var local_reduced =
      samples.GroupBy(s => (int)(s[0]), (k, list) => Clustering(k, list));

    var global_reduced =
      local_reduced.GroupBy(s => 0, (k, list) => Reduction(k, list));

    var next_samples =
      global_reduced.CoGroupBy(samples,
                               s => (int)(s[0][0]),
                               s => (int)(s[0]),
                               (k, means, new_samples) => Synch(k, means, new_samples));

    Console.Out.WriteLine("**End Advance**");
    Console.Out.Flush();

    return next_samples;
  }

  private List<Means> Clustering(int k, IEnumerable<Sample> list) {
    var means = new Means();
    lock(lock_) {
      means = means_;
      if (p_counter_ == 0) {
        clustering_stamp_ = Stopwatch.GetTimestamp();
      }
    }

    int count = 0;
    Means reduced = new Means();
    for (int i = 0; i < cluster_num_; ++i) {
      Mean r = new Mean();
      for (int j = 0; j < dimension_ + 1; j++) {
        // the first element represents the weight.
        r.Add(0);
      }
      reduced.Add(r);
    }

    if (spin_wait_ == 0) {
      foreach (var s in list) {
        int idx = 0;
        double dist = -1;
        for (int i = 0; i < cluster_num_; ++i) {
          double temp_dist = SquareDistance(s, 1, means[i], 0);
          if ((temp_dist < dist) || (dist == -1)) {
            idx = i;
            dist = temp_dist;
          }
        }

        reduced[idx][0] = reduced[idx][0] +  1;
        var temp = reduced[idx];
        VectorAccWithScale(ref temp, 1, s, 1, 1);
        reduced[idx] = temp;
        count++;
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
      counter_1_.Add(count);
      if (++p_counter_ == pnpw_) {
        p_counter_ = 0;
        clustering_elapsed_ = (double)(Stopwatch.GetTimestamp() - clustering_stamp_) / (double)(Stopwatch.Frequency);
      }
    }

    var out_list = new List<Means>();
    out_list.Add(reduced);
    return out_list;
  }

  private List<Means> Reduction(int k, IEnumerable<Means> list) {
    int count = 0;

    Means reduced = new Means();
    for (int i = 0; i < cluster_num_; ++i) {
      Mean r = new Mean();
      for (int j = 0; j < dimension_ + 1; j++) {
        // the first element represents the weight.
        r.Add(0);
      }
      reduced.Add(r);
    }

    foreach (var m in list) {
      for (int i = 0; i < cluster_num_; ++i) {
        reduced[i][0] = reduced[i][0] + m[i][0];
        var temp = reduced[i];
        VectorAccWithScale(ref temp, 1, m[i], 1, 1);
        reduced[i] = temp;
      }
      ++count;
    }

    lock(lock_) {
      counter_2_.Add(count);
    }

    var out_list = new List<Means>();
    for (int p = 0; p < partition_num_; ++p) {

      Means synch = new Means();
      for (int i = 0; i < cluster_num_; ++i) {
        Mean m = new Mean();
        // the fisrt element used for partitioning
        m.Add(p);
        for (int j = 0; j < dimension_ + 1; j++) {
          // the rest of the elemnts, weight and the poist
          m.Add(reduced[i][j]);
        }
        synch.Add(m);
      }
      out_list.Add(synch);
    }
    return out_list;

  }

  private IEnumerable<Sample> Synch(int k, IEnumerable<Means> means, IEnumerable<Sample> samples) {
    int count = 0;
    foreach (var m in means) {
      lock(lock_) {
        for (int i = 0; i < cluster_num_; ++i) {
          for (int j = 0; j < dimension_; ++j) {
            if (m[i][1] != 0) {
              means_[i][j] = m[i][j+2] / m[i][1];
            }
          }
        }
      }
      count++;
    }
    lock(lock_) {
      counter_3_.Add(count);
      if (++p_counter_ == pnpw_) {
        p_counter_ = 0;
        loop_index_++;
        var elapsed = (double)(Stopwatch.GetTimestamp() - loop_stamp_) / (double)(Stopwatch.Frequency);
        loop_stamp_ = Stopwatch.GetTimestamp();
        Console.Out.WriteLine("Loop {0:D2} clustering(ms): {1:F2} total(ms): {2:F2} ",
                              loop_index_, 1000 * clustering_elapsed_, 1000 * elapsed);
        Console.Out.Flush();
      }
    }
    return samples;
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

  public static double VectorDot(List<double> v1, int of1, List<double>v2, int of2) {
    Debug.Assert((v1.Count - of1) == (v2.Count - of2));
    double result = 0;
    for (int i = 0; i < (v1.Count - of1); ++i) {
      result += v1[i+of1] * v2[i+of2];
    }
    return result;
  }

  public static double SquareDistance(List<double> v1, int of1, List<double>v2, int of2) {
    Debug.Assert((v1.Count - of1) == (v2.Count - of2));
    double result = 0;
    for (int i = 0; i < (v1.Count - of1); ++i) {
      result += (v1[i+of1] - v2[i+of2]) * (v1[i+of1] - v2[i+of2]);
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


  public static string PrintListList<T>(List<List<T>> list) {
    int length = list.Count;
    if (length == 0) {
      return "[]";
    }
    string str = "[";
    for (int i = 0; i < length; i++) {
      str += PrintList(list[i]);
      str += ",\n";
    }
    str += "]";
    return str;
  }

}


