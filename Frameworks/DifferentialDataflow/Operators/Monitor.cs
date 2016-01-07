/*
 * Naiad ver. 0.4
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0 
 *
 * THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT
 * LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR
 * A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
 *
 * See the Apache Version 2.0 License for specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Research.Naiad.Utilities;
using Microsoft.Research.Naiad.Dataflow;
using Microsoft.Research.Naiad.Dataflow.StandardVertices;

namespace Microsoft.Research.Naiad.Frameworks.DifferentialDataflow.Operators
{
    internal class Monitor<R, T> : UnaryVertex<Weighted<R>, Weighted<R>, T>
        where R : IEquatable<R>
        where T : Time<T>
    {
        public Action<int, List<Pair<Weighted<R>,T>>> action;
        public List<Pair<Weighted<R>,T>> list;

        Int64 count;
        T leastTime;


        public override void OnReceive(Message<Weighted<R>, T> message)
        {
            this.NotifyAt(message.time);
            if (count == 0 || leastTime.CompareTo(message.time) > 0)
                leastTime = message.time;

            for (int i = 0; i < message.length; i++)
            {
                if (action != null)
                    list.Add(message.payload[i].PairWith(message.time));

                count++;
            }

            this.Output.Send(message);
        }

        public override void OnNotify(T time)
        {
            if (action != null)
            {
                action(VertexId, list);
                list.Clear();
            }
            else
            { 
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", this.VertexId, count, leastTime, System.DateTime.Now);

                count = 0;
            }
        }

        public Monitor(int index, Stage<T> collection, bool immutableInput, Action<int, List<Pair<Weighted<R>,T>>> a) : base(index, collection)
        {
            action = a;
            list = new List<Pair<Weighted<R>,T>>();
        }
    }
}