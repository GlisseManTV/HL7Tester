## v2.0.22

### 🔗 Keep Connection Open
> One connection for the whole batch — faster, lighter, more realistic.
- New toggle next to Start/Stop: reuse a single TCP connection for all messages
- Eliminates the overhead of opening and closing a connection for every single message
- Ideal for high-throughput stress testing where connection setup is the bottleneck

### 📅 Event Dates Per Step
> Control exactly when each event happens in your workflow.
- Set a specific date/time for any step (format: yyyyMMddHHmm)
- Link a step's event time to a previous step using `@N` (e.g., `@2` = same time as step 2)
- Leave empty to use the current time automatically

### 🏥 Multiple Locations at Once
> Admit patients across several rooms or units in one batch.
- Enter multiple values separated by commas in Room, Bed, Unit, or Floor (e.g., `301A, 302B, 308A`)
- Each patient gets a random location from your list
- Perfect for simulating mass admissions across a hospital wing

### 👤 Force Patient Sex
> Override the random sex when your endpoint expects a specific one.
- New "Sex" picker: leave empty for random, or force M or F for all patients
- Useful when testing language-specific endpoints that expect a fixed gender

### 📋 Smarter Step Limits
> More flexibility in building your workflow.
- You can now run a batch with just 1 step (previously minimum was 2)
- Maximum increased to 7 steps for more complex scenarios
- Quick access to logs from the status bar
