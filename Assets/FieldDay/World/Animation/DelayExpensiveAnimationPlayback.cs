using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using FieldDay.Scenes;
using ScriptableBake;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.ParticleSystem;

namespace FieldDay.Animation {
    /// <summary>
    /// Disables animators and particle systems until the scene is ready,
    /// </summary>
    [AddComponentMenu("")]
    public sealed class DelayExpensiveAnimationPlayback : SceneCustomData {
        [SerializeField] private ParticleSystem[] m_ParticleSystems;
        [SerializeField] private Animator[] m_Animators;
        [SerializeField] private SkinnedMeshRenderer[] m_SkinnedMeshes;

#if UNITY_EDITOR
        public override bool Build(Scene scene) {
            List<Animator> animators = new List<Animator>(100);
            scene.GetAllComponents<Animator>(animators);
            animators.RemoveAll((a) => !a.enabled);

            m_Animators = animators.ToArray();

            foreach(var animator in animators) {
                animator.enabled = false;
                Baking.SetDirty(animator);
            }

            List<ParticleSystem> particles = new List<ParticleSystem>(100);
            scene.GetAllComponents<ParticleSystem>(particles);
            particles.RemoveAll((p) => !p.emission.enabled || !p.main.playOnAwake);

            m_ParticleSystems = particles.ToArray();

            foreach(var particle in particles) {
                var emission = particle.emission;
                emission.enabled = false;
                var main = particle.main;
                main.playOnAwake = false;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Baking.SetDirty(particle);
            }

            List<SkinnedMeshRenderer> skinnedMeshes = new List<SkinnedMeshRenderer>(64);
            scene.GetAllComponents<SkinnedMeshRenderer>(skinnedMeshes);
            skinnedMeshes.RemoveAll((s) => !s.enabled);

            m_SkinnedMeshes = skinnedMeshes.ToArray();

            foreach (var skinned in skinnedMeshes) {
                skinned.enabled = false;
                Baking.SetDirty(skinned);
            }

            return m_ParticleSystems.Length > 0 || m_Animators.Length > 0 || m_SkinnedMeshes.Length > 0;
        }
#endif // UNITY_EDITOR

        public override void OnReady() {
            foreach(var animator in m_Animators) {
                if (animator) {
                    animator.enabled = true;
                }
            }

            foreach (var skinnedMesh in m_SkinnedMeshes) {
                if (skinnedMesh) {
                    skinnedMesh.enabled = true;
                }
            }

            foreach (var part in m_ParticleSystems) {
                if (part) {
                    var emission = part.emission;
                    emission.enabled = true;
                    var main = part.main;
                    main.playOnAwake = true;
                    part.Play();
                }
            }

            m_Animators = null;
            m_ParticleSystems = null;
            m_SkinnedMeshes = null;
        }
    }
}