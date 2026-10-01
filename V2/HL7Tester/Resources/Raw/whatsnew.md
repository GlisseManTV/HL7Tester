## v2.0.21

### 📋 All ADT Messages in Batch Send
> Every admission, movement, and discharge type is now available for stress testing.
- Pick from all 30 ADT message types (A01 to A40) in the step editor
- Build complex workflows with any combination of admission, movement, and discharge steps
- No more limited selection — what you can send manually, you can now batch

### 🔀 Merge Patient Records in Batch
> Test patient record merges (A18/A40) with a dedicated target ID field.
- When you add a merge step, a "New Patient ID" field appears automatically
- Leave it empty to merge into the same patient, or enter a specific ID
- Perfect for testing merge scenarios in production-like conditions

### ⚠️ Clear Error Messages on Rejected Messages
> When a server rejects your message, you now see exactly why.
- The send log shows "NACK" with the server's error description
- No more guessing why a message was rejected — the reason is right there
- Works for both "rejected" (AR) and "accepted with errors" (AE) responses
