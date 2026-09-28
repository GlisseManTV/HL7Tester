## v2.0.19

### 🚀 Batch Send — Stress Test Your HL7 Service
> Send batches of HL7 messages in parallel to test your service under real production load.
- Pick a predefined workflow (e.g., A01 → A02 → A08 → A03) or build your own
- Send 10, 15, 20+ patients simultaneously — each follows the workflow step by step
- Set a global delay between all messages to simulate realistic traffic
- Stop at any time with one click

### ✏️ Flexible Workflow Editor
> Customize every step of your workflow with per-step overrides.
- Reorder steps with up/down arrows, delete any step with the trash icon
- For movement steps: override Room, Bed, Unit, or Floor per step
- For A31 (Update Patient): override Name and Surname per step
- Empty fields fall back to your global values automatically

### 🎲 Realistic Patient Data
> Every patient gets unique randomized identity data for realistic testing.
- Random names, surnames, patient IDs, and admission numbers
- Random sex and birth dates within a realistic range
- Location (Room/Bed/Unit/Floor) stays consistent across all patients
