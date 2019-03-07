/*
   Copyright 2015-2018 Dennis Piatkowski <dennis@dwulf.com>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License. 
 */

#if DEBUG
// DDP This is locking up for some reason, so leaving disabled.
//#define TRACK_CALLERS
#endif

using UnityEngine;
using System;
using System.Collections;

namespace DWulf.Async
{
    /// <summary>
    /// a standardized wrapper to returning an asynchronous result to a consumer.
    /// </summary>
    /// <remarks>
    /// You will never create one of these yourself - only a Answer (producer side) can do so.
    /// This is expected to return only one result! Do not re-use Promises!
    /// If you want a multi-callback, you're probably better off with a pure delegate.
    /// 
    /// This is designed to avoid virtual methods as much as possible.
    /// We 'split' at this point - Promise can be used interchangeably with Promise&lt;T&gt;, but
    /// the corresponding Answer classes cannot.
    /// </remarks>
    public class Promise
    {
        #region Implementation

        /// <summary>
        /// This is what callers 'wait' for
        /// </summary>
        public bool isDone { get; protected set; }

        /// <summary>
        /// Any error of the requested task is stored here.
        /// </summary>
        /// <remarks>
        /// Only valid after isDone. Consumers are required to process errors after every call!
        /// Error logging should be done by consumer (not producer), if it deems it necessary.
        /// </remarks>
        public Exception Error { get; protected set; }

        #endregion

        #region Utility

        /// <summary>
        /// Only valid after isDone
        /// </summary>
        public bool Failed { get { return Error != null; } }

        /// <summary>
        /// Only valid after isDone
        /// </summary>
        public bool Success { get { return Error == null; } }

        /// <summary>
        /// wait for isDone.
        /// </summary>
        /// <remarks>
        /// Remember that the caller needs to start the coroutine, so it is attached to the caller.
        /// This way if the caller is destroyed, the coroutine goes with it.
        /// </remarks>
        public IEnumerator WaitForCompletion()
        {
            while (!isDone)
                yield return null;
        }

        /// <summary>
        /// Set a callback. 
        /// </summary>
        /// <remarks>
        /// Preferred is watching isDone to complete because we want to avoid boomerang hell.
        /// WARNING - This may call your object *after* it has been destroyed!
        /// Safer is to use WaitForCompletion.
        /// If proc is already done, expect it to be called immediately.
        /// </remarks>
        public Promise OnSucceed(Action doneProc)
        {
            if (isDone)
            {
                // We explicitly don't bother with all the exception wrapping here as the 
                // stack trace will already indicate the originating caller's true location.
                if (Success)
                    doneProc();
            } else
                _doneProc = doneProc;
            
            return this;
        }

        /// <summary>
        /// Set an error callback. 
        /// </summary>
        /// <remarks>
        /// Preferred is watching isDone to complete because we want to avoid boomerang hell.
        /// WARNING - This may call your object *after* it has been destroyed!
        /// Safer is to use WaitForCompletion.
        /// If proc is already done, expect it to be called immediately.
        /// </remarks>
        public Promise OnFail(Action<Exception> doneProc)
        {
            if (isDone)
            {
                // We explicitly don't bother with all the exception wrapping here as the 
                // stack trace will already indicate the originating caller's true location.
                if (Failed)
                    doneProc(Error);
            } else
                _errProc = doneProc;
            
            return this;
        }

        /// <summary>
        /// Set a callback for both succeed or fail.
        /// </summary>
        /// <remarks>
        /// Preferred is watching isDone to complete because we want to avoid boomerang hell.
        /// WARNING - This may call your object *after* it has been destroyed!
        /// Safer is to use WaitForCompletion.
        /// If proc is already done, expect it to be called immediately.
        /// </remarks>
        public Promise OnFinally(Action doneProc)
        {
            if (isDone)
            {
                // We explicitly don't bother with all the exception wrapping here as the 
                // stack trace will already indicate the originating caller's true location.
                doneProc();

            } else
                _finallyProc = doneProc;
            
            return this;
        }

        #endregion

        #region Details

        /// <summary>
        /// make this inaccessible to normal new()
        /// </summary>
        protected Promise()
        {
        }

        /// <summary>
        /// called when successfully done, no return value
        /// </summary>
        protected Action _doneProc;

        /// <summary>
        /// Called when error occurs.
        /// </summary>
        protected Action<Exception> _errProc;

        /// <summary>
        /// called when done (success or fail), no return value
        /// </summary>
        protected Action _finallyProc;

        /// <summary>
        /// Last known error, or null
        /// </summary>
        protected Exception _error;

        #endregion
    }

    /// <summary>
    /// a standardized wrapper to returning an asynchronous result to a consumer.
    /// </summary>
    /// <remarks>
    /// The type of T should be the raw return value you are wrapping in this Promise.
    /// For more complicated types, you should declare a class/struct and pass that as T. Do not subclass!
    /// This will allow callers to hold local copies of the result without need of a wrapping Answer.
    /// You will never create one of these yourself - only a Answer (producer side) can do so.
    /// This is expected to return only one result! Do not re-use Promises!
    /// If you want a multi-callback, you're probably better off with a pure delegate.
    /// 
    /// Many methods are duplicated here (compared to baseclass) only for documentation purposes.
    /// This is designed to avoid virtual methods as much as possible.
    /// </remarks>
    public class Promise<T> : Promise
    {
        #region Implementation

        /// <summary>
        /// the actual value we manage.
        /// Only valid after isDone.
        /// </summary>
        public T Value { get; protected set; }

        #endregion


        #region Utility

        /// <summary>
        /// Only valid after isDone
        /// </summary>
        public new bool Failed { get { return Error != null; } }

        /// <summary>
        /// Only valid after isDone
        /// </summary>
        public new bool Success { get { return Error == null; } }

        /// <summary>
        /// wait for isDone.
        /// </summary>
        /// <remarks>
        /// Remember that the caller needs to start the coroutine, so it is attached to the caller.
        /// This way if the caller is destroyed, the coroutine goes with it.
        /// </remarks>
        public new IEnumerator WaitForCompletion()
        {
            while (!isDone)
                yield return null;
        }

        /// <summary>
        /// Set a callback. 
        /// </summary>
        /// <remarks>
        /// Preferred is watching isDone to complete because we want to avoid boomerang hell.
        /// WARNING - This may call your object *after* it has been destroyed!
        /// Safer is to use WaitForCompletion.
        /// If proc is already done, expect it to be called immediately.
        /// </remarks>
        public Promise<T> OnSucceed(Action<T> result)
        {
            if (isDone)
            {
                // We explicitly don't bother with all the exception wrapping here as the 
                // stack trace will already indicate the originating caller's true location.
                if (Success)
                    result(Value);
            }
            else
                _valProc = result;

            return this;
        }

        /// <summary>
        /// Set an error callback. 
        /// </summary>
        /// <remarks>
        /// Preferred is watching isDone to complete because we want to avoid boomerang hell.
        /// WARNING - This may call your object *after* it has been destroyed!
        /// Safer is to use WaitForCompletion.
        /// If proc is already done, expect it to be called immediately.
        /// </remarks>
        public new Promise<T> OnFail(Action<Exception> doneProc)
        {
            if (isDone)
            {
                // We explicitly don't bother with all the exception wrapping here as the 
                // stack trace will already indicate the originating caller's true location.
                if (Failed)
                    doneProc(Error);
            } else
                _errProc = doneProc;

            return this;            
        }

        #endregion

        #region Details

        /// <summary>
        /// Last saved 'return' value
        /// </summary>
        protected Action<T> _valProc;

        #endregion
    }

    /// <summary>
    /// a standardized wrapper for a producer to create an asynchronous result.
    /// </summary>
    /// <remarks>
    /// This is expected to return only one result! Do not re-use Answers!
    /// If you want a multi-callback, you're probably better off with a pure delegate.
    /// 
    /// This is designed to avoid virtual methods as much as possible.
    /// </remarks>
    public class Answer : Promise
    {
        // these methods all return the embedded Promise so you can simply return item.Succeed()

        #region Implementation

        /// <summary>
        /// Mark task completed
        /// </summary>
        /// <returns>self</returns>
        public Answer Succeed()
        {
#if DEBUG
            if (isDone)
                throw Wrap(new InvalidOperationException("Trying to complete a task twice."));
#endif

            isDone = true;

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _doneProc;
                _doneProc = null;
                if (proc != null)
                    proc();
            }

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _finallyProc;
                _finallyProc = null;
                if (proc != null)
                    proc();
            }

            return this;
        }

        /// <summary>
        /// Mark task failed
        /// </summary>
        /// <returns>self</returns>
        /// <remarks>
        /// be sure to give us some kinda idea what went wrong.
        /// </remarks>
        public Answer Fail(Exception e)
        {
#if DEBUG
            if (isDone)
                throw Wrap(new InvalidOperationException("Trying to set error on a completed task."));
#endif

            Error = Wrap(e ?? new ApplicationException("Fail with no exception"));
            isDone = true;

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _errProc;
                _errProc = null;
                if (proc != null)
                    proc(Error);
            }
            
            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _finallyProc;
                _finallyProc = null;
                if (proc != null)
                    proc();
            }

            return this;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Mark task failed
        /// </summary>
        /// <returns>self</returns>
        /// <remarks>
        /// be sure to give us some kinda idea what went wrong.
        /// </remarks>
        public Promise Fail(string errorFormat, params object[] args)
        {
            string message = string.Format(errorFormat, args);
            return Fail(new ApplicationException(message));
        }

        #endregion

        #region Debugging

#if TRACK_CALLERS

        /// <summary>
        /// Create an answer
        /// </summary>
        public Answer()
        {
            // keep a record of who started this shindig
            _originator = new System.Diagnostics.StackTrace();
        }

        /// <summary>
        /// take an exception and add stack trace about who originally requested the task.
        /// </summary>
        /// <remarks>
        /// From MSDN: "When an exception X is thrown as a direct result of a previous exception Y, 
        /// the InnerException property of X should contain a reference to Y."
        /// 
        /// We abuse InnerException so that the information about the originating caller is retained,
        /// however it won't be a proper stack trace (or even in the right order).
        /// </remarks>
        private Exception Wrap(Exception e)
        {
            if (_originator == null)
                return e;

            return new Exception("Original caller:"+_originator.ToString(), e);
        }

        private System.Diagnostics.StackTrace _originator;
#else
        /// <summary>
        /// take an exception and add stack trace about who originally requested the task.
        /// </summary>
        private Exception Wrap(Exception e)
        {
            return e;
        }
#endif

        #endregion
    }

    /// <summary>
    /// a standardized wrapper for a producer to create an asynchronous result.
    /// </summary>
    /// <remarks>
    /// This is expected to return only one result! Do not re-use Answers!
    /// If you want a multi-callback, you're probably better off with a pure delegate.
    /// 
    /// This is designed to avoid virtual methods as much as possible.
    /// </remarks>
    public class Answer<T> : Promise<T>
    {

        // these methods all return the embedded Promise so you can simply return item.Succeed(blah)


#region Implementation

        /// <summary>
        /// Mark task completed
        /// </summary>
        /// <returns>self</returns>
        public Answer<T> Succeed(T value)
        {
#if DEBUG
            if (isDone)
                throw new InvalidOperationException("Trying to complete a task twice.");
#endif
            Value = value;
            isDone = true;

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _doneProc;
                _doneProc = null;
                if (proc != null)
                    proc();
            }

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc2 = _valProc;
                _valProc = null;
                if (proc2 != null)
                    proc2(value);
            }

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _finallyProc;
                _finallyProc = null;
                if (proc != null)
                    proc();
            }

            return this;
        }

        /// <summary>
        /// Mark task failed
        /// </summary>
        /// <returns>self</returns>
        /// <remarks>
        /// be sure to give us some kinda idea what went wrong.
        /// </remarks>
        public Answer<T> Fail(Exception e)
        {
#if DEBUG
            if (isDone)
                throw new InvalidOperationException("Trying to set error on a completed task.");
#endif

            Error = e ?? new ApplicationException("Fail with no exception");
            isDone = true;

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _errProc;
                _errProc = null;
                if (proc != null)
                    proc(Error);
            }

            {
                // This might be overly paranoid, but i think I should swap these.
                var proc = _finallyProc;
                _finallyProc = null;
                if (proc != null)
                    proc();
            }

            return this;
        }

#endregion

#region Utility

        /// <summary>
        /// Mark task failed
        /// </summary>
        /// <returns>self</returns>
        /// <remarks>
        /// be sure to give us some kinda idea what went wrong.
        /// </remarks>
        public Answer<T> Fail(string errorFormat, params object[] args)
        {
            string message = string.Format(errorFormat, args);
            return Fail(new ApplicationException(message));
        }

#endregion
    }

    internal interface ActiveCoroutine
    {
        Coroutine GetActiveImp();
    }

    /// <summary>
    /// Thrown when a Looping coroutine exits unexpectedly
    /// </summary>
    public class LoopCancelledException : Exception
    {
        
    }

    /// <summary>
    /// A wrapper on a coroutine to make it catch exceptions properly
    /// </summary>
    public class CoroutineWrapper : Answer, ActiveCoroutine
    {
        /// <summary>
        /// Unity's instance that callers can yield to
        /// </summary>
        /// <remarks>
        /// Each instance of CoroutineWrapper should only be used by one coroutine, and then set here.
        /// </remarks>
        public Coroutine ActiveImp;

        // Exception handling from: http://twistedoakstudios.com/blog/Post83_coroutines-more-than-you-want-to-know

        /// <summary>
        /// Run a one-shot coroutine safely
        /// </summary>
        /// <param name="coroutine"></param>
        public IEnumerator Wrap(IEnumerator coroutine)
        {
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        ActiveImp = null;
                        if (!isDone)
                            Succeed();  // caller may have already succeeded/failed us.
                        yield break;
                    }
                } catch (Exception e)
                {
                    ActiveImp = null;
                    Fail(e);
                    yield break;
                }

                object yielded = coroutine.Current;
                if (yielded == null)
                    yield return null;
                else
                {
                    if (yielded is YieldInstruction)
                    {
                        yield return yielded;
                        continue;
                    }
                    if (yielded is ActiveCoroutine)
                    {
                        // Eventually, wait directly on the child wrapper, don't pass back to Unity.
                        // For now, yield in naive way.
                        // Even if GetActiveImp returns null, still ok to wait for next frame.
                        yield return (yielded as ActiveCoroutine).GetActiveImp();
                        continue;
                    }
                    // check this after ActiveCoroutine
                    if (yielded is Promise)
                    {
                        // Not a coroutine itself, so wait for it manually.
                        var promise = yielded as Promise;
                        while (!promise.isDone)
                            yield return null;
                        continue;
                    }
                    if (yielded is Exception)
                    {
                        ActiveImp = null;
                        Fail((Exception)yielded);
                        yield break;
                    }

                    // if we don't know the yield type, this is a code error.
                    // Sadly we can only catch these at runtime, not compile time.
#if DEBUG
                    Fail( new InvalidOperationException("Routine returned unknown yield type " + yielded.GetType().Name));
#endif
                }
            }
        }

        /// <summary>
        /// Run a coroutine that should never stop, throwing LoopCancelled error if so.
        /// </summary>
        /// <remarks>
        /// "Proper" shutdown is for the object to be destroyed.
        /// It is safe for the caller to restart the loop from the Error proc if desired.
        /// </remarks>
        /// <param name="coroutine"></param>
        /// <returns></returns>
        public IEnumerator Forever(IEnumerator coroutine)
        {
            IEnumerator real = Wrap(coroutine);
            while (true)
            {
                if (!real.MoveNext())
                {
                    Fail(new LoopCancelledException());
                    yield break;
                }
                yield return real.Current;
            }
        }

        /// <summary>
        /// Get the coroutine we're being used from.
        /// </summary>
        public Coroutine GetActiveImp()
        {
            return ActiveImp;
        }
    }

    /// <summary>
    /// A wrapper on a coroutine to make it catch exceptions properly
    /// </summary>
    public class CoroutineWrapper<T> : Answer<T>, ActiveCoroutine
    {
        /// <summary>
        /// Unity's instance that callers can yield to
        /// </summary>
        /// <remarks>
        /// Each instance of CoroutineWrapper should only be used by one coroutine, and then set here.
        /// </remarks>
        public Coroutine ActiveImp;

        // Exception handling from: http://twistedoakstudios.com/blog/Post83_coroutines-more-than-you-want-to-know

        /// <summary>
        /// Run a one-shot coroutine safely
        /// </summary>
        /// <param name="coroutine"></param>
        public IEnumerator Wrap(IEnumerator coroutine)
        {
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        ActiveImp = null;
                        if (!isDone)
                            Fail(new Exception("Coroutine completed without passing a result."));
                        yield break;
                    }
                } 
                catch (Exception e)
                {
                    ActiveImp = null;
                    Fail(e);
                    yield break;
                }

                object yielded = coroutine.Current;
                if (yielded == null)
                    yield return null;
                else
                {
                    if (yielded is YieldInstruction)
                    {
                        yield return yielded;
                        continue;
                    }
                    if (yielded is ActiveCoroutine)
                    {
                        // Eventually, wait directly on the child wrapper, don't pass back to Unity.
                        // For now, yield in naive way.
                        // Even if GetActiveImp returns null, still ok to wait for next frame.
                        yield return (yielded as ActiveCoroutine).GetActiveImp();
                        continue;
                    }
                    // check this after ActiveCoroutine
                    if (yielded is Promise)
                    {
                        // Not a coroutine itself, so wait for it manually.
                        var promise = yielded as Promise;
                        while (!promise.isDone)
                            yield return null;
                        continue;
                    }
                    if (yielded.GetType() == typeof(T))
                    {
                        // we might allow the coroutine to continue?
                        // We need to give this some thought.
                        ActiveImp = null;
                        Succeed((T)yielded);
                        yield break;    
                    }
                    if (yielded is Exception)
                    {
                        ActiveImp = null;
                        Fail((Exception)yielded);
                        yield break;
                    }

                    // if we don't know the yield type, this is a code error.
                    // Sadly we can only catch these at runtime, not compile time.
#if DEBUG
                    Fail(new InvalidOperationException("Routine returned unknown yield type " + yielded.GetType().Name));
#endif
                }
            }
        }

        /// <summary>
        /// Get the coroutine we're being used from.
        /// </summary>
        public Coroutine GetActiveImp()
        {
            return ActiveImp;
        }
    }

    /// <summary>
    /// Add coroutineWrapper support to Monobehaviours
    /// </summary>
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Replacement for StartCoroutine, using our wrappers.
        /// </summary>
        public static CoroutineWrapper<T> Run<T>(this MonoBehaviour self, IEnumerator task)
        {
            var ret = new CoroutineWrapper<T>();
            self.StartCoroutine(ret.Wrap(task));
            return ret;
        }

        /// <summary>
        /// Replacement for StartCoroutine, using our wrappers.
        /// </summary>
        public static CoroutineWrapper Run(this MonoBehaviour self, IEnumerator task)
        {
            var ret = new CoroutineWrapper();
            self.StartCoroutine(ret.Wrap(task));
            return ret;
        }
    }

}
