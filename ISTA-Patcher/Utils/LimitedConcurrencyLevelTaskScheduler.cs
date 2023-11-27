// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2023 TautCony

namespace ISTA_Patcher.Utils;

// Provides a task scheduler that ensures a maximum concurrency level while
// running on top of the thread pool.
// from: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-7.0
public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
   // Indicates whether the current thread is processing work items.
   [ThreadStatic]
   private static bool _currentThreadIsProcessingItems;

  // The list of tasks to be executed
   private readonly LinkedList<Task> _tasks = new(); // protected by lock(_tasks)

   // Indicates whether the scheduler is currently processing work items.
   private int _delegatesQueuedOrRunning;

   // Creates a new instance with the specified degree of parallelism.
   public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
   {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);
        this.MaximumConcurrencyLevel = maxDegreeOfParallelism;
   }

   // Queues a task to the scheduler.
   protected sealed override void QueueTask(Task task)
   {
      // Add the task to the list of tasks to be processed.  If there aren't enough
      // delegates currently queued or running to process tasks, schedule another.
       lock (this._tasks)
       {
           this._tasks.AddLast(task);
           if (this._delegatesQueuedOrRunning < this.MaximumConcurrencyLevel)
           {
               ++this._delegatesQueuedOrRunning;
               this.NotifyThreadPoolOfPendingWork();
           }
       }
   }

   // Inform the ThreadPool that there's work to be executed for this scheduler.
   private void NotifyThreadPoolOfPendingWork()
   {
       ThreadPool.UnsafeQueueUserWorkItem(
           _ =>
       {
           // Note that the current thread is now processing work items.
           // This is necessary to enable inlining of tasks into this thread.
           _currentThreadIsProcessingItems = true;
           try
           {
               // Process all available items in the queue.
               while (true)
               {
                   Task item = null;
                   lock (this._tasks)
                   {
                       // When there are no more items to be processed,
                       // note that we're done processing, and get out.
                       if (this._tasks.Count == 0)
                       {
                           --this._delegatesQueuedOrRunning;
                           break;
                       }

                       // Get the next item from the queue
                       if (this._tasks.First != null)
                       {
                           item = this._tasks.First.Value;
                       }

                       this._tasks.RemoveFirst();
                   }

                   if (item != null)
                   {
                       // Execute the task we pulled out of the queue
                       this.TryExecuteTask(item);
                   }
               }
           }

           // We're done processing items on the current thread
           finally
           {
               _currentThreadIsProcessingItems = false;
           }
       },
           null);
   }

   // Attempts to execute the specified task on the current thread.
   protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
   {
       // If this thread isn't already processing a task, we don't support inlining
       if (!_currentThreadIsProcessingItems)
       {
           return false;
       }

       // If the task was previously queued, remove it from the queue
       if (taskWasPreviouslyQueued)
       {
           // Try to run the task.
           return this.TryDequeue(task) && this.TryExecuteTask(task);
       }

       return this.TryExecuteTask(task);
   }

   // Attempt to remove a previously scheduled task from the scheduler.
   protected sealed override bool TryDequeue(Task task)
   {
       lock (this._tasks)
       {
           return this._tasks.Remove(task);
       }
   }

   // The maximum concurrency level allowed by this scheduler.
   public sealed override int MaximumConcurrencyLevel { get; }

   // Gets an enumerable of the tasks currently scheduled on this scheduler.
   protected sealed override IEnumerable<Task> GetScheduledTasks()
   {
       var lockTaken = false;
       try
       {
           Monitor.TryEnter(this._tasks, ref lockTaken);
           if (lockTaken)
           {
               return this._tasks;
           }
           else
           {
               throw new NotSupportedException();
           }
       }
       finally
       {
           if (lockTaken)
           {
               Monitor.Exit(this._tasks);
           }
       }
   }
}
