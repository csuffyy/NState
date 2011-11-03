﻿namespace NState
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;
    using NBasicExtensionMethod;
    using Newtonsoft.Json;
    using NSure;
    using ArgumentNullException = NHelpfulException.FrameworkExceptions.ArgumentNullException;

    /// <summary>
    ///   Enables specification of valid state changes to be applied to object
    ///   instances.
    /// </summary>
    [Serializable]
    public class StateMachine<TStatefulDomainObject, TState> :
        IStateMachine<TStatefulDomainObject, TState>
        where TStatefulDomainObject : IStateful<TStatefulDomainObject, TState>
        where TState : State
    {
        protected readonly
            IEnumerable
                <IStateTransition<TStatefulDomainObject, TState>>
            StateTransitions;

        public StateMachine(
            IEnumerable
                <IStateTransition<TStatefulDomainObject, TState>>
                stateTransitions,
            TState startState,
            List<IStateMachine> childStateMachines = null,
            List<IStateMachine> parentStateMachines = null)
        {
            Ensure.That(stateTransitions.IsNotNull(), "stateTransitions not supplied.");

            StateTransitions = stateTransitions;
            ChildStateMachines = childStateMachines ?? new List<IStateMachine>();
            ParentStateMachines = parentStateMachines ?? new List<IStateMachine>();
            StartState = startState;
            CurrentState = startState;
        }

        public TState StartState { get; set; }

        public TState CurrentState { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public List<IStateMachine> ChildStateMachines { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public List<IStateMachine> ParentStateMachines { get; set; }

        public Dictionary<DateTime, IStateTransition<TStatefulDomainObject, TState>> History { get; set; }

        /// <summary>
        ///   WIP - hierarchy!
        /// </summary>
        public TStatefulDomainObject PerformTransition(TStatefulDomainObject statefulDomainObject, TState targetState,
                                                       dynamic dto = default(dynamic))
        {
            Ensure.That<ArgumentNullException>(statefulDomainObject.IsNotNull(),
                                               "statefulDomainObject not supplied.");

            OnRaiseBeforeEveryTransition();

            try
            {
                if (CurrentState != targetState) //make this explicit?
                {
                    statefulDomainObject = StateTransitions.First(
                        t =>
                        t.StartState.DistinctBy(s => s.Name == CurrentState.Name).Any() &&
                        t.EndState.DistinctBy(e => e.Name == targetState.Name).Any()).
                        Transition
                        (statefulDomainObject, targetState, dto);

                    CurrentState = targetState;
                }
                OnRaiseAfterEveryTransition();

                return statefulDomainObject;
            }
            catch (Exception)
            {
                var i = new InvalidStateTransitionException<TState>(CurrentState, targetState);

                throw i;
            }
        }

        public event EventHandler RaiseBeforeEveryTransitionEvent;

        public event EventHandler RaiseAfterEveryTransitionEvent;

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected virtual void OnRaiseBeforeEveryTransition()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = RaiseBeforeEveryTransitionEvent;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                // Format the string to send inside the CustomEventArgs parameter
                //e.Message += String.Format(" at {0}", DateTime.Now.ToString());

                // Use the () operator to raise the event.
                handler(this, new EventArgs());
            }
        }

        protected virtual void OnRaiseAfterEveryTransition()
        {
            var handler = RaiseAfterEveryTransitionEvent;

            if (handler != null) // Event will be null if there are no subscribers
            {
                handler(this, new EventArgs());
            }
        }
    }
}