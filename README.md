# How to install

## For Windows:
Download the app directly from the [Microsoft Store](https://apps.microsoft.com/store/detail/9PJM3TWNDG20?cid=DevShareMCLPCS).
Or download the latest package [here](https://github.com/GlisseManTV/HL7Tester/releases/latest)

(Due to the certification process, there may be a delay between the publication of the GitHub release and its availability on the Ms Store.)


## For MacOS:
Download the latest package [here](https://github.com/GlisseManTV/HL7Tester/releases/latest)

### How to install on macOS (unsigned app)

Because HL7Tester for macOS is currently **not signed / not notarized** by Apple, macOS Gatekeeper may block it after downloading from the internet.

You can install and run it using one of the following options:

#### Option 1 – Recommended for most users (no Terminal required)
1. Download the latest `HL7Tester_MacOS_*.dmg` from the GitHub release page.
2. Open the DMG.
3. Drag `HL7Tester.app` into the `Applications` folder.
4. Open the `Applications` folder.
5. **Right‑click** (or Ctrl+click) on `HL7Tester.app` and select **Open**.
6. macOS will show a warning that the app is from an unidentified developer – click **Open** again.
7. Next launches can be done normally (double‑click), without the warning.

#### Option 2 – For advanced users (Terminal)
1. Download the latest `HL7Tester_MacOS_*.dmg`.
2. Open the Terminal and remove the quarantine attribute from the DMG:

   ```bash
   cd ~/Downloads
   xattr -d com.apple.quarantine HL7Tester_MacOS_*.dmg
   ```

3. Open the DMG and drag `HL7Tester.app` into `Applications`.
4. If macOS still blocks the app in `Applications`, you can also remove the quarantine attribute from the app itself:

   ```bash
   sudo xattr -d -r com.apple.quarantine /Applications/HL7Tester.app
   ```

After that, HL7Tester should launch normally.


# HL7Generator

User Friendly HL7 Generator trought a great UI

This Windows / MacOS application allow you to generate and send HL7 V2.3 messages trought a user friendly UI.


## Preview


### Main UI &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;dark &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; light

<img width="341" height="347" alt="image" src="https://github.com/user-attachments/assets/8f5b61ae-8f93-4670-a068-1b96bf4390d2" />
<img width="341" height="347" alt="image" src="https://github.com/user-attachments/assets/49006c1c-f29e-411b-9531-693402e0fa9e" />





### Settings &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;dark &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; light


<img width="341" height="347" alt="image" src="https://github.com/user-attachments/assets/6cb6aa78-7b65-4c6e-9d0b-180813993b4e" />
<img width="341" height="347" alt="image" src="https://github.com/user-attachments/assets/73d61442-3a51-42db-8eb4-5dd63ae57f3d" />




### Example of use : 


**To be recorded**


# Disclaimer

Thanks to NHapi for the beautiful library (https://github.com/nHapiNET/nHapi)

This project is NOT affiliated with the HL7 organization. This software just conforms to the HL7 2.x specifications.




