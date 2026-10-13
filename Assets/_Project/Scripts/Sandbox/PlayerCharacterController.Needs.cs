using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public partial class PlayerCharacterController
    {
        [Header("Necessidades e Sobrevivência (HUD)")]
        [Range(0f, 100f)] public float stamina = 100f;
        public float maxStamina = 100f;

        [Range(0f, 100f)] public float hunger = 90f;
        public float maxHunger = 100f;

        [Range(0f, 100f)] public float sleep = 85f;
        public float maxSleep = 100f;

        public bool isSprinting { get; private set; } = false;

        /// <summary>
        /// Atualiza as necessidades biológicas do jogador (Estamina, Fome, Sono).
        /// </summary>
        public void UpdatePlayerNeeds()
        {
            float dt = Time.deltaTime;

            if (isDriving)
            {
                // Em veículos: estamina regenera normalmente; sono e fome decaem devagar
                stamina = Mathf.Min(maxStamina, stamina + 25f * dt);
                hunger = Mathf.Max(0f, hunger - 0.03f * dt);
                sleep = Mathf.Max(0f, sleep - 0.02f * dt);
                isSprinting = false;
            }
            else
            {
                // A pé: consumo de estamina ao correr
                if (isSprinting)
                {
                    stamina = Mathf.Max(0f, stamina - 18f * dt);
                }
                else
                {
                    // Regeneração de estamina quando andando ou parado
                    stamina = Mathf.Min(maxStamina, stamina + 22f * dt);
                }

                // Fome e sono decaem continuamente
                float hungerDrainRate = isSprinting ? 0.07f : 0.035f;
                hunger = Mathf.Max(0f, hunger - hungerDrainRate * dt);
                sleep = Mathf.Max(0f, sleep - 0.025f * dt);
            }
        }

        public void ConsumeFood(float amount)
        {
            hunger = Mathf.Clamp(hunger + amount, 0f, maxHunger);
        }

        public void Rest(float amount)
        {
            sleep = Mathf.Clamp(sleep + amount, 0f, maxSleep);
            stamina = maxStamina;
        }
    }
}
