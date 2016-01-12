
using System;
using System.Diagnostics;
using System.Collections.Generic;

using Sample = System.Collections.Generic.List<double>;
using Weight = System.Collections.Generic.List<double>;


public class Gradient
{
  // properties for implementing the logic of the application.
  public Int32 dimension_;
  public Int32 iteration_num_;
  public double sample_num_m_;
  public Int64 sample_num_;
  public Weight weight_;
  public List<Sample> samples_;

  Gradient (Int32 dimension,
            Int32 iteration_num,
            double sample_num_m) {
    dimension_ = dimension;
    iteration_num_ = iteration_num;
    sample_num_m_ = sample_num_m;
    sample_num_ = (Int64)(sample_num_m_ * 1e6);

    weight_ = new Weight();
    for (int j = 0; j < dimension; j++) {
      weight_.Add(1);
    }
    samples_ = new List<Sample>();
  }

  static void PrintHelp() {
    string str = "\nUsage:";
    str += "\n  Gradient.exe <dimension> <iteration_num> <sample_num in million>";
    str += "\n";
    Console.Out.WriteLine(str);
    Console.Out.Flush();
  }

  static void Main(string[] args) {
    if (args.Length != 3) {
      PrintHelp();
      return;
    } else if (args.Length == 1) {
      if (args[0] == "--help" || args[0] == "-h") {
        PrintHelp();
        return;
      }
    }

    Int32 dimension = Int32.Parse(args[0]);
    Int32 iteration_num = Int32.Parse(args[1]);
    double sample_num_m = Convert.ToDouble(args[2]);

    Console.Out.WriteLine("dimension: " + dimension);
    Console.Out.WriteLine("iteration_num: " + iteration_num);
    Console.Out.WriteLine("sample_num in million: " + sample_num_m);
    Console.Out.Flush();


    Gradient g =
      new Gradient(dimension,
                   iteration_num,
                   sample_num_m);

    long stamp = 0;
    double elapsed = 0;

    stamp = Stopwatch.GetTimestamp();
    g.GenerateSamples();
    elapsed = (double)(Stopwatch.GetTimestamp() - stamp) / (double)(Stopwatch.Frequency);
    Console.Out.WriteLine("Generating samples(ms): {0:F2} ", 1000 * elapsed);


    for (int i = 0; i < g.iteration_num_; ++i) {
      stamp = Stopwatch.GetTimestamp();
      g.Advance();
      elapsed = (double)(Stopwatch.GetTimestamp() - stamp) / (double)(Stopwatch.Frequency);
      Console.Out.WriteLine("Iteration {0:D2} elapsed(ms): {1:F2} ", i, 1000 * elapsed);

    }
  }

  private void Advance() {
    Weight reduced = new Weight(new double[dimension_]);
    foreach (var s in samples_) {
      var scale = (1 / (1 + Math.Exp(s[1] * VectorDot(s, 1, weight_, 0))) - 1) * s[0];
      VectorAccWithScale(ref reduced, 0, s, 1, scale);
    }
    for (int i = 0; i < dimension_; ++i) {
      weight_[i] = reduced[i];
    }
  }

  public void GenerateSamples() {
    var random = new Random(0);
    for (int i = 0; i < sample_num_; i++) {
      Sample sample = new Sample();
      // the first element specifies the label +1/-1
      sample.Add((2 * (i % 2)) - 1);

      for (int j = 0; j < dimension_; j++) {
        sample.Add(random.Next(133));
      }

      samples_.Add(sample);
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


