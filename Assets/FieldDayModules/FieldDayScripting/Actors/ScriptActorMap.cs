using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Set of all registered actors and a map of all named registered actors.
    /// </summary>
    public class ScriptActorMap<TActor> where TActor : class, ILeafActor {
        public readonly HashSet<ILeafActor> AllActors;
        public readonly Dictionary<StringHash32, TActor> NamedActors;

        public ScriptActorMap(int capacity) {
            AllActors = new HashSet<ILeafActor>(capacity);
            NamedActors = MapUtils.Create<StringHash32, TActor>(capacity);
        }

        /// <summary>
        /// Registers an actor.
        /// Returns if the actor was successfully registered.
        /// </summary>
        public bool Register(TActor actor) {
            if (!AllActors.Add(actor)) {
                return false;
            }

            StringHash32 id = actor.Id;
            if (!id.IsEmpty) {
                if (NamedActors.TryGetValue(id, out TActor existing)) {
                    Log.Error("[ScriptActorMap] Duplicate actor ids - another actor with id '{0}' registered", id.ToDebugString());
                } else {
                    NamedActors.Add(id, actor);
                    Log.Debug("[ScriptActorMap] Registered actor with id '{0}'", id.ToDebugString());
                }
            } else {
                Log.Debug("[ScriptActorMap] Registered unnamed actor with GameObject name '{0}'", AssetUtility.NameOf(actor));
            }
            return true;
        }

        /// <summary>
        /// Deregisters an actor.
        /// Returns if the actor was successfully removed.
        /// </summary>
        public bool Deregister(TActor actor) {
            if (!AllActors.Remove(actor)) {
                return false;
            }

            StringHash32 id = actor.Id;
            if (!id.IsEmpty) {
                if (NamedActors.TryGetValue(id, out TActor existing) && existing == actor) {
                    NamedActors.Remove(id);
                    Log.Debug("[ScriptActorMap] Deregistered actor with id '{0}'", id.ToDebugString());
                }
            } else {
                Log.Debug("[ScriptActorMap] Deregistered unnamed actor with GameObject name '{0}'", AssetUtility.NameOf(actor));
            }
            return true;
        }

        /// <summary>
        /// Attempts to retrieve the actor with the given id.
        /// </summary>
        public bool TryGet(StringHash32 id, out TActor actor) {
            return NamedActors.TryGetValue(id, out actor);
        }
    }
}