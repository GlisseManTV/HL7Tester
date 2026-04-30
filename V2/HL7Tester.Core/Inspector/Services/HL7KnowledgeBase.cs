namespace HL7Tester.Core.Inspector.Services;

/// <summary>
/// Static knowledge base providing human-readable descriptions for HL7 v2 segments,
/// fields, and components. Covers the most common segments used in clinical applications.
/// Keyed by "SEGMENT-FIELDINDEX" for fields and "DATATYPE.COMPINDEX" for components.
/// </summary>
public static class HL7KnowledgeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    // Segment descriptions
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string> SegmentNames
        = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MSH"] = "Message Header",
        ["EVN"] = "Event Type",
        ["PID"] = "Patient Identification",
        ["PD1"] = "Patient Additional Demographic",
        ["NK1"] = "Next of Kin / Associated Parties",
        ["PV1"] = "Patient Visit",
        ["PV2"] = "Patient Visit - Additional Information",
        ["ROL"] = "Role",
        ["DB1"] = "Disability",
        ["OBX"] = "Observation / Result",
        ["AL1"] = "Patient Allergy Information",
        ["DG1"] = "Diagnosis",
        ["DRG"] = "Diagnosis Related Group",
        ["GT1"] = "Guarantor",
        ["IN1"] = "Insurance",
        ["IN2"] = "Insurance - Additional Information",
        ["ACC"] = "Accident",
        ["UB1"] = "UB82",
        ["UB2"] = "UB92 Data",
        ["ORC"] = "Common Order",
        ["OBR"] = "Observation Request",
        ["NTE"] = "Notes and Comments",
        ["MSA"] = "Message Acknowledgment",
        ["ERR"] = "Error",
        ["QRD"] = "Query Definition",
        ["SCH"] = "Scheduling Activity Information",
        ["RGS"] = "Resource Group",
        ["AIG"] = "Appointment Information - General Resource",
        ["AIL"] = "Appointment Information - Location Resource",
        ["AIP"] = "Appointment Information - Personnel Resource",
        ["AIS"] = "Appointment Information - Service",
        ["SAC"] = "Specimen Container Detail",
        ["SPM"] = "Specimen",
        ["TXA"] = "Transcription Document Header",
        ["SFT"] = "Software Segment",
        ["UAC"] = "User Authentication Credential",
        ["ARV"] = "Access Restriction",
        ["PRD"] = "Provider Data",
        ["CTD"] = "Contact Data",
        ["QAK"] = "Query Acknowledgment",
        ["QPD"] = "Query Parameter Definition",
        ["RCP"] = "Response Control Parameter",
        ["DSC"] = "Continuation Pointer",
        ["DSP"] = "Display Data",
        ["BHS"] = "Batch Header Segment",
        ["BTS"] = "Batch Trailer Segment",
        ["FHS"] = "File Header Segment",
        ["FTS"] = "File Trailer Segment",
        ["ZBE"] = "Custom: Bed Assignment",
        ["ZFA"] = "Custom: Facility Assignment",
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Field names and data types  (key = "SEGMENT-FIELDINDEX")
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, (string Name, string DataType, bool Required)> FieldInfo
        = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── MSH ──────────────────────────────────────────────────────────────
        ["MSH-1"]  = ("Field Separator",                   "ST",  true),
        ["MSH-2"]  = ("Encoding Characters",               "ST",  true),
        ["MSH-3"]  = ("Sending Application",               "HD",  false),
        ["MSH-4"]  = ("Sending Facility",                  "HD",  false),
        ["MSH-5"]  = ("Receiving Application",             "HD",  false),
        ["MSH-6"]  = ("Receiving Facility",                "HD",  false),
        ["MSH-7"]  = ("Date/Time of Message",              "DTM", true),
        ["MSH-8"]  = ("Security",                          "ST",  false),
        ["MSH-9"]  = ("Message Type",                      "MSG", true),
        ["MSH-10"] = ("Message Control ID",                "ST",  true),
        ["MSH-11"] = ("Processing ID",                     "PT",  true),
        ["MSH-12"] = ("Version ID",                        "VID", true),
        ["MSH-13"] = ("Sequence Number",                   "NM",  false),
        ["MSH-14"] = ("Continuation Pointer",              "ST",  false),
        ["MSH-15"] = ("Accept Acknowledgment Type",        "ID",  false),
        ["MSH-16"] = ("Application Acknowledgment Type",   "ID",  false),
        ["MSH-17"] = ("Country Code",                      "ID",  false),
        ["MSH-18"] = ("Character Set",                     "ID",  false),
        ["MSH-19"] = ("Principal Language of Message",     "CWE", false),
        ["MSH-20"] = ("Alternate Character Set Handling",  "ID",  false),
        ["MSH-21"] = ("Message Profile Identifier",        "EI",  false),

        // ── EVN ──────────────────────────────────────────────────────────────
        ["EVN-1"]  = ("Event Type Code",                   "ID",  false),
        ["EVN-2"]  = ("Recorded Date/Time",                "DTM", true),
        ["EVN-3"]  = ("Date/Time Planned Event",           "DTM", false),
        ["EVN-4"]  = ("Event Reason Code",                 "IS",  false),
        ["EVN-5"]  = ("Operator ID",                       "XCN", false),
        ["EVN-6"]  = ("Event Occurred",                    "DTM", false),
        ["EVN-7"]  = ("Event Facility",                    "HD",  false),

        // ── MSA ──────────────────────────────────────────────────────────────
        ["MSA-1"]  = ("Acknowledgment Code",               "ID",  true),
        ["MSA-2"]  = ("Message Control ID",                "ST",  true),
        ["MSA-3"]  = ("Text Message",                      "ST",  false),
        ["MSA-4"]  = ("Expected Sequence Number",          "NM",  false),
        ["MSA-5"]  = ("Delayed Acknowledgment Type",       "ID",  false),
        ["MSA-6"]  = ("Error Condition",                   "CWE", false),

        // ── ERR ──────────────────────────────────────────────────────────────
        ["ERR-1"]  = ("Error Code and Location",           "ELD", false),
        ["ERR-2"]  = ("Error Location",                    "ERL", false),
        ["ERR-3"]  = ("HL7 Error Code",                    "CWE", true),
        ["ERR-4"]  = ("Severity",                          "ID",  true),
        ["ERR-5"]  = ("Application Error Code",            "CWE", false),
        ["ERR-6"]  = ("Application Error Parameter",       "ST",  false),
        ["ERR-7"]  = ("Diagnostic Information",            "TX",  false),
        ["ERR-8"]  = ("User Message",                      "TX",  false),
        ["ERR-9"]  = ("Inform Person Indicator",           "IS",  false),
        ["ERR-10"] = ("Override Type",                     "CWE", false),
        ["ERR-11"] = ("Override Reason Code",              "CWE", false),
        ["ERR-12"] = ("Help Desk Contact Point",           "XTN", false),

        // ── PID ──────────────────────────────────────────────────────────────
        ["PID-1"]  = ("Set ID - PID",                       "SI",  false),
        ["PID-2"]  = ("Patient ID (External)",              "CX",  false),
        ["PID-3"]  = ("Patient Identifier List",            "CX",  true),
        ["PID-4"]  = ("Alternate Patient ID",               "CX",  false),
        ["PID-5"]  = ("Patient Name",                       "XPN", true),
        ["PID-6"]  = ("Mother's Maiden Name",               "XPN", false),
        ["PID-7"]  = ("Date/Time of Birth",                 "DTM", false),
        ["PID-8"]  = ("Administrative Sex",                 "IS",  false),
        ["PID-9"]  = ("Patient Alias",                      "XPN", false),
        ["PID-10"] = ("Race",                               "CWE", false),
        ["PID-11"] = ("Patient Address",                    "XAD", false),
        ["PID-12"] = ("County Code",                        "IS",  false),
        ["PID-13"] = ("Phone Number - Home",                "XTN", false),
        ["PID-14"] = ("Phone Number - Business",            "XTN", false),
        ["PID-15"] = ("Primary Language",                   "CWE", false),
        ["PID-16"] = ("Marital Status",                     "CWE", false),
        ["PID-17"] = ("Religion",                           "CWE", false),
        ["PID-18"] = ("Patient Account Number",             "CX",  false),
        ["PID-19"] = ("SSN Number - Patient",               "ST",  false),
        ["PID-20"] = ("Driver's License Number - Patient",  "DLN", false),
        ["PID-21"] = ("Mother's Identifier",                "CX",  false),
        ["PID-22"] = ("Ethnic Group",                       "CWE", false),
        ["PID-23"] = ("Birth Place",                        "ST",  false),
        ["PID-24"] = ("Multiple Birth Indicator",           "ID",  false),
        ["PID-25"] = ("Birth Order",                        "NM",  false),
        ["PID-26"] = ("Citizenship",                        "CWE", false),
        ["PID-27"] = ("Veterans Military Status",           "CWE", false),
        ["PID-28"] = ("Nationality",                        "CWE", false),
        ["PID-29"] = ("Patient Death Date and Time",        "DTM", false),
        ["PID-30"] = ("Patient Death Indicator",            "ID",  false),
        ["PID-31"] = ("Identity Unknown Indicator",         "ID",  false),
        ["PID-32"] = ("Identity Reliability Code",          "IS",  false),
        ["PID-33"] = ("Last Update Date/Time",              "DTM", false),
        ["PID-34"] = ("Last Update Facility",               "HD",  false),
        ["PID-35"] = ("Species Code",                       "CWE", false),
        ["PID-36"] = ("Breed Code",                         "CWE", false),
        ["PID-37"] = ("Strain",                             "ST",  false),
        ["PID-38"] = ("Production Class Code",              "CWE", false),
        ["PID-39"] = ("Tribal Citizenship",                 "CWE", false),

        // ── PD1 ──────────────────────────────────────────────────────────────
        ["PD1-1"]  = ("Living Dependency",                  "IS",  false),
        ["PD1-2"]  = ("Living Arrangement",                 "IS",  false),
        ["PD1-3"]  = ("Patient Primary Facility",           "XON", false),
        ["PD1-4"]  = ("Patient Primary Care Provider",      "XCN", false),
        ["PD1-5"]  = ("Student Indicator",                  "IS",  false),
        ["PD1-6"]  = ("Handicap",                           "IS",  false),
        ["PD1-7"]  = ("Living Will Code",                   "IS",  false),
        ["PD1-8"]  = ("Organ Donor Code",                   "IS",  false),
        ["PD1-9"]  = ("Separate Bill",                      "ID",  false),
        ["PD1-10"] = ("Duplicate Patient",                  "CX",  false),
        ["PD1-11"] = ("Publicity Code",                     "CWE", false),
        ["PD1-12"] = ("Protection Indicator",               "ID",  false),
        ["PD1-13"] = ("Protection Indicator Effective Date","DT",  false),
        ["PD1-14"] = ("Place of Worship",                   "XON", false),
        ["PD1-15"] = ("Advance Directive Code",             "CWE", false),
        ["PD1-16"] = ("Immunization Registry Status",       "IS",  false),
        ["PD1-17"] = ("Immunization Registry Effective Date","DT", false),
        ["PD1-18"] = ("Publicity Code Effective Date",      "DT",  false),
        ["PD1-19"] = ("Military Branch",                    "CWE", false),
        ["PD1-20"] = ("Military Rank/Grade",                "IS",  false),
        ["PD1-21"] = ("Military Status",                    "CWE", false),

        // ── NK1 ──────────────────────────────────────────────────────────────
        ["NK1-1"]  = ("Set ID - NK1",                       "SI",  true),
        ["NK1-2"]  = ("Name",                               "XPN", false),
        ["NK1-3"]  = ("Relationship",                       "CWE", false),
        ["NK1-4"]  = ("Address",                            "XAD", false),
        ["NK1-5"]  = ("Phone Number",                       "XTN", false),
        ["NK1-6"]  = ("Business Phone Number",              "XTN", false),
        ["NK1-7"]  = ("Contact Role",                       "CWE", false),
        ["NK1-8"]  = ("Start Date",                         "DT",  false),
        ["NK1-9"]  = ("End Date",                           "DT",  false),
        ["NK1-10"] = ("Next of Kin / Associated Parties Job Title", "ST", false),
        ["NK1-11"] = ("Next of Kin / Associated Parties Job Code",  "JCC", false),
        ["NK1-12"] = ("Next of Kin / Associated Parties Employee Number", "CX", false),
        ["NK1-13"] = ("Organization Name - NK1",            "XON", false),
        ["NK1-14"] = ("Marital Status",                     "CWE", false),
        ["NK1-15"] = ("Administrative Sex",                 "IS",  false),
        ["NK1-16"] = ("Date/Time of Birth",                 "DTM", false),
        ["NK1-17"] = ("Living Dependency",                  "IS",  false),
        ["NK1-18"] = ("Ambulatory Status",                  "IS",  false),
        ["NK1-19"] = ("Citizenship",                        "CWE", false),
        ["NK1-20"] = ("Primary Language",                   "CWE", false),
        ["NK1-21"] = ("Living Arrangement",                 "IS",  false),
        ["NK1-22"] = ("Publicity Code",                     "CWE", false),
        ["NK1-23"] = ("Protection Indicator",               "ID",  false),
        ["NK1-24"] = ("Student Indicator",                  "IS",  false),
        ["NK1-25"] = ("Religion",                           "CWE", false),
        ["NK1-26"] = ("Mother's Maiden Name",               "XPN", false),
        ["NK1-27"] = ("Nationality",                        "CWE", false),
        ["NK1-28"] = ("Ethnic Group",                       "CWE", false),
        ["NK1-29"] = ("Contact Reason",                     "CWE", false),
        ["NK1-30"] = ("Contact Person's Name",              "XPN", false),
        ["NK1-31"] = ("Contact Person's Telephone Number",  "XTN", false),
        ["NK1-32"] = ("Contact Person's Address",           "XAD", false),
        ["NK1-33"] = ("Next of Kin / Associated Party Identifiers", "CX", false),
        ["NK1-34"] = ("Job Status",                         "IS",  false),
        ["NK1-35"] = ("Race",                               "CWE", false),
        ["NK1-36"] = ("Handicap",                           "IS",  false),
        ["NK1-37"] = ("Contact Person Social Security Number", "ST", false),

        // ── PV1 ──────────────────────────────────────────────────────────────
        ["PV1-1"]  = ("Set ID - PV1",                       "SI",  false),
        ["PV1-2"]  = ("Patient Class",                      "IS",  true),
        ["PV1-3"]  = ("Assigned Patient Location",          "PL",  false),
        ["PV1-4"]  = ("Admission Type",                     "IS",  false),
        ["PV1-5"]  = ("Preadmit Number",                    "CX",  false),
        ["PV1-6"]  = ("Prior Patient Location",             "PL",  false),
        ["PV1-7"]  = ("Attending Doctor",                   "XCN", false),
        ["PV1-8"]  = ("Referring Doctor",                   "XCN", false),
        ["PV1-9"]  = ("Consulting Doctor",                  "XCN", false),
        ["PV1-10"] = ("Hospital Service",                   "IS",  false),
        ["PV1-11"] = ("Temporary Location",                 "PL",  false),
        ["PV1-12"] = ("Preadmit Test Indicator",            "IS",  false),
        ["PV1-13"] = ("Re-admission Indicator",             "IS",  false),
        ["PV1-14"] = ("Admit Source",                       "IS",  false),
        ["PV1-15"] = ("Ambulatory Status",                  "IS",  false),
        ["PV1-16"] = ("VIP Indicator",                      "IS",  false),
        ["PV1-17"] = ("Admitting Doctor",                   "XCN", false),
        ["PV1-18"] = ("Patient Type",                       "IS",  false),
        ["PV1-19"] = ("Visit Number",                       "CX",  false),
        ["PV1-20"] = ("Financial Class",                    "FC",  false),
        ["PV1-21"] = ("Charge Price Indicator",             "IS",  false),
        ["PV1-22"] = ("Courtesy Code",                      "IS",  false),
        ["PV1-23"] = ("Credit Rating",                      "IS",  false),
        ["PV1-24"] = ("Contract Code",                      "IS",  false),
        ["PV1-25"] = ("Contract Effective Date",            "DT",  false),
        ["PV1-26"] = ("Contract Amount",                    "NM",  false),
        ["PV1-27"] = ("Contract Period",                    "NM",  false),
        ["PV1-28"] = ("Interest Code",                      "IS",  false),
        ["PV1-29"] = ("Transfer to Bad Debt Code",          "IS",  false),
        ["PV1-30"] = ("Transfer to Bad Debt Date",          "DT",  false),
        ["PV1-31"] = ("Bad Debt Agency Code",               "IS",  false),
        ["PV1-32"] = ("Bad Debt Transfer Amount",           "NM",  false),
        ["PV1-33"] = ("Bad Debt Recovery Amount",           "NM",  false),
        ["PV1-34"] = ("Delete Account Indicator",           "IS",  false),
        ["PV1-35"] = ("Delete Account Date",                "DT",  false),
        ["PV1-36"] = ("Discharge Disposition",              "IS",  false),
        ["PV1-37"] = ("Discharged to Location",             "DLD", false),
        ["PV1-38"] = ("Diet Type",                          "CWE", false),
        ["PV1-39"] = ("Servicing Facility",                 "IS",  false),
        ["PV1-40"] = ("Bed Status",                         "IS",  false),
        ["PV1-41"] = ("Account Status",                     "IS",  false),
        ["PV1-42"] = ("Pending Location",                   "PL",  false),
        ["PV1-43"] = ("Prior Temporary Location",           "PL",  false),
        ["PV1-44"] = ("Admit Date/Time",                    "DTM", false),
        ["PV1-45"] = ("Discharge Date/Time",                "DTM", false),
        ["PV1-46"] = ("Current Patient Balance",            "NM",  false),
        ["PV1-47"] = ("Total Charges",                      "NM",  false),
        ["PV1-48"] = ("Total Adjustments",                  "NM",  false),
        ["PV1-49"] = ("Total Payments",                     "NM",  false),
        ["PV1-50"] = ("Alternate Visit ID",                 "CX",  false),
        ["PV1-51"] = ("Visit Indicator",                    "IS",  false),
        ["PV1-52"] = ("Other Healthcare Provider",          "XCN", false),

        // ── PV2 ──────────────────────────────────────────────────────────────
        ["PV2-1"]  = ("Prior Pending Location",             "PL",  false),
        ["PV2-2"]  = ("Accommodation Code",                 "CWE", false),
        ["PV2-3"]  = ("Admit Reason",                       "CWE", false),
        ["PV2-4"]  = ("Transfer Reason",                    "CWE", false),
        ["PV2-5"]  = ("Patient Valuables",                  "ST",  false),
        ["PV2-6"]  = ("Patient Valuables Location",         "ST",  false),
        ["PV2-7"]  = ("Visit User Code",                    "IS",  false),
        ["PV2-8"]  = ("Expected Admit Date/Time",           "DTM", false),
        ["PV2-9"]  = ("Expected Discharge Date/Time",       "DTM", false),
        ["PV2-10"] = ("Estimated Length of Inpatient Stay", "NM",  false),
        ["PV2-11"] = ("Actual Length of Inpatient Stay",    "NM",  false),
        ["PV2-12"] = ("Visit Description",                  "ST",  false),
        ["PV2-13"] = ("Referral Source Code",               "XCN", false),
        ["PV2-14"] = ("Previous Service Date",              "DT",  false),
        ["PV2-15"] = ("Employment Illness Related Indicator","ID", false),
        ["PV2-16"] = ("Purge Status Code",                  "IS",  false),
        ["PV2-17"] = ("Purge Status Date",                  "DT",  false),
        ["PV2-18"] = ("Special Program Code",               "IS",  false),
        ["PV2-19"] = ("Retention Indicator",                "ID",  false),
        ["PV2-20"] = ("Expected Number of Insurance Plans", "NM",  false),
        ["PV2-21"] = ("Visit Publicity Code",               "IS",  false),
        ["PV2-22"] = ("Visit Protection Indicator",         "ID",  false),
        ["PV2-23"] = ("Clinic Organization Name",           "XON", false),
        ["PV2-24"] = ("Patient Status Code",                "IS",  false),
        ["PV2-25"] = ("Visit Priority Code",                "IS",  false),
        ["PV2-26"] = ("Previous Treatment Date",            "DT",  false),
        ["PV2-27"] = ("Expected Discharge Disposition",     "IS",  false),
        ["PV2-28"] = ("Signature on File Date",             "DT",  false),
        ["PV2-29"] = ("First Similar Illness Date",         "DT",  false),
        ["PV2-30"] = ("Patient Charge Adjustment Code",     "CWE", false),
        ["PV2-31"] = ("Recurring Service Code",             "IS",  false),
        ["PV2-32"] = ("Billing Media Code",                 "ID",  false),
        ["PV2-33"] = ("Expected Surgery Date/Time",         "DTM", false),
        ["PV2-34"] = ("Military Partnership Code",          "ID",  false),
        ["PV2-35"] = ("Military Non-Availability Code",     "ID",  false),
        ["PV2-36"] = ("Newborn Baby Indicator",             "ID",  false),
        ["PV2-37"] = ("Baby Detained Indicator",            "ID",  false),

        // ── ORC ──────────────────────────────────────────────────────────────
        ["ORC-1"]  = ("Order Control",                      "ID",  true),
        ["ORC-2"]  = ("Placer Order Number",                "EI",  false),
        ["ORC-3"]  = ("Filler Order Number",                "EI",  false),
        ["ORC-4"]  = ("Placer Group Number",                "EI",  false),
        ["ORC-5"]  = ("Order Status",                       "ID",  false),
        ["ORC-6"]  = ("Response Flag",                      "ID",  false),
        ["ORC-7"]  = ("Quantity/Timing",                    "TQ",  false),
        ["ORC-8"]  = ("Parent Order",                       "EIP", false),
        ["ORC-9"]  = ("Date/Time of Transaction",           "DTM", false),
        ["ORC-10"] = ("Entered By",                         "XCN", false),
        ["ORC-11"] = ("Verified By",                        "XCN", false),
        ["ORC-12"] = ("Ordering Provider",                  "XCN", false),
        ["ORC-13"] = ("Enterer's Location",                 "PL",  false),
        ["ORC-14"] = ("Call Back Phone Number",             "XTN", false),
        ["ORC-15"] = ("Order Effective Date/Time",          "DTM", false),
        ["ORC-16"] = ("Order Control Code Reason",          "CWE", false),
        ["ORC-17"] = ("Entering Organization",              "CWE", false),
        ["ORC-18"] = ("Entering Device",                    "CWE", false),
        ["ORC-19"] = ("Action By",                          "XCN", false),
        ["ORC-20"] = ("Advanced Beneficiary Notice Code",   "CWE", false),
        ["ORC-21"] = ("Ordering Facility Name",             "XON", false),
        ["ORC-22"] = ("Ordering Facility Address",          "XAD", false),
        ["ORC-23"] = ("Ordering Facility Phone Number",     "XTN", false),
        ["ORC-24"] = ("Ordering Provider Address",          "XAD", false),
        ["ORC-25"] = ("Order Status Modifier",              "CWE", false),

        // ── OBR ──────────────────────────────────────────────────────────────
        ["OBR-1"]  = ("Set ID - OBR",                       "SI",  false),
        ["OBR-2"]  = ("Placer Order Number",                "EI",  false),
        ["OBR-3"]  = ("Filler Order Number",                "EI",  false),
        ["OBR-4"]  = ("Universal Service Identifier",       "CWE", true),
        ["OBR-5"]  = ("Priority - OBR",                     "ID",  false),
        ["OBR-6"]  = ("Requested Date/Time",                "DTM", false),
        ["OBR-7"]  = ("Observation Date/Time",              "DTM", false),
        ["OBR-8"]  = ("Observation End Date/Time",          "DTM", false),
        ["OBR-9"]  = ("Collection Volume",                  "CQ",  false),
        ["OBR-10"] = ("Collector Identifier",               "XCN", false),
        ["OBR-11"] = ("Specimen Action Code",               "ID",  false),
        ["OBR-12"] = ("Danger Code",                        "CWE", false),
        ["OBR-13"] = ("Relevant Clinical Information",      "ST",  false),
        ["OBR-14"] = ("Specimen Received Date/Time",        "DTM", false),
        ["OBR-15"] = ("Specimen Source",                    "SPS", false),
        ["OBR-16"] = ("Ordering Provider",                  "XCN", false),
        ["OBR-17"] = ("Order Callback Phone Number",        "XTN", false),
        ["OBR-18"] = ("Placer Field 1",                     "ST",  false),
        ["OBR-19"] = ("Placer Field 2",                     "ST",  false),
        ["OBR-20"] = ("Filler Field 1",                     "ST",  false),
        ["OBR-21"] = ("Filler Field 2",                     "ST",  false),
        ["OBR-22"] = ("Results Rpt/Status Chng - Date/Time","DTM", false),
        ["OBR-23"] = ("Charge to Practice",                 "MOC", false),
        ["OBR-24"] = ("Diagnostic Service Section ID",      "ID",  false),
        ["OBR-25"] = ("Result Status",                      "ID",  false),
        ["OBR-26"] = ("Parent Result",                      "PRL", false),
        ["OBR-27"] = ("Quantity/Timing",                    "TQ",  false),
        ["OBR-28"] = ("Result Copies To",                   "XCN", false),
        ["OBR-29"] = ("Parent Number",                      "EIP", false),
        ["OBR-30"] = ("Transportation Mode",                "ID",  false),
        ["OBR-31"] = ("Reason for Study",                   "CWE", false),
        ["OBR-32"] = ("Principal Result Interpreter",       "NDL", false),
        ["OBR-33"] = ("Assistant Result Interpreter",       "NDL", false),
        ["OBR-34"] = ("Technician",                         "NDL", false),
        ["OBR-35"] = ("Transcriptionist",                   "NDL", false),
        ["OBR-36"] = ("Scheduled Date/Time",                "DTM", false),
        ["OBR-37"] = ("Number of Sample Containers",        "NM",  false),
        ["OBR-38"] = ("Transport Logistics of Collected Sample", "CWE", false),
        ["OBR-39"] = ("Collector's Comment",                "CWE", false),
        ["OBR-40"] = ("Transport Arrangement Responsibility","CWE",false),
        ["OBR-41"] = ("Transport Arranged",                 "ID",  false),
        ["OBR-42"] = ("Escort Required",                    "ID",  false),
        ["OBR-43"] = ("Planned Patient Transport Comment",  "CWE", false),
        ["OBR-44"] = ("Procedure Code",                     "CNE", false),
        ["OBR-45"] = ("Procedure Code Modifier",            "CNE", false),
        ["OBR-46"] = ("Placer Supplemental Service Information", "CWE", false),
        ["OBR-47"] = ("Filler Supplemental Service Information", "CWE", false),

        // ── OBX ──────────────────────────────────────────────────────────────
        ["OBX-1"]  = ("Set ID - OBX",                       "SI",  false),
        ["OBX-2"]  = ("Value Type",                         "ID",  false),
        ["OBX-3"]  = ("Observation Identifier",             "CWE", true),
        ["OBX-4"]  = ("Observation Sub-ID",                 "ST",  false),
        ["OBX-5"]  = ("Observation Value",                  "varies",false),
        ["OBX-6"]  = ("Units",                              "CWE", false),
        ["OBX-7"]  = ("References Range",                   "ST",  false),
        ["OBX-8"]  = ("Abnormal Flags",                     "IS",  false),
        ["OBX-9"]  = ("Probability",                        "NM",  false),
        ["OBX-10"] = ("Nature of Abnormal Test",            "ID",  false),
        ["OBX-11"] = ("Observation Result Status",          "ID",  true),
        ["OBX-12"] = ("Effective Date of Reference Range",  "DTM", false),
        ["OBX-13"] = ("User Defined Access Checks",         "ST",  false),
        ["OBX-14"] = ("Date/Time of the Observation",       "DTM", false),
        ["OBX-15"] = ("Producer's ID",                      "CWE", false),
        ["OBX-16"] = ("Responsible Observer",               "XCN", false),
        ["OBX-17"] = ("Observation Method",                 "CWE", false),
        ["OBX-18"] = ("Equipment Instance Identifier",      "EI",  false),
        ["OBX-19"] = ("Date/Time of the Analysis",          "DTM", false),
        ["OBX-20"] = ("Observation Site",                   "CWE", false),
        ["OBX-21"] = ("Observation Instance Identifier",    "EI",  false),
        ["OBX-23"] = ("Performing Organization Name",       "XON", false),
        ["OBX-24"] = ("Performing Organization Address",    "XAD", false),
        ["OBX-25"] = ("Performing Organization Medical Director","XCN",false),

        // ── NTE ──────────────────────────────────────────────────────────────
        ["NTE-1"]  = ("Set ID - NTE",                       "SI",  false),
        ["NTE-2"]  = ("Source of Comment",                  "ID",  false),
        ["NTE-3"]  = ("Comment",                            "FT",  false),
        ["NTE-4"]  = ("Comment Type",                       "CWE", false),

        // ── AL1 ──────────────────────────────────────────────────────────────
        ["AL1-1"]  = ("Set ID - AL1",                       "SI",  true),
        ["AL1-2"]  = ("Allergen Type Code",                 "CWE", false),
        ["AL1-3"]  = ("Allergen Code/Mnemonic/Description", "CWE", true),
        ["AL1-4"]  = ("Allergy Severity Code",              "CWE", false),
        ["AL1-5"]  = ("Allergy Reaction Code",              "ST",  false),
        ["AL1-6"]  = ("Identification Date",                "DT",  false),

        // ── DG1 ──────────────────────────────────────────────────────────────
        ["DG1-1"]  = ("Set ID - DG1",                       "SI",  true),
        ["DG1-2"]  = ("Diagnosis Coding Method",            "ID",  false),
        ["DG1-3"]  = ("Diagnosis Code - DG1",               "CWE", false),
        ["DG1-4"]  = ("Diagnosis Description",              "ST",  false),
        ["DG1-5"]  = ("Diagnosis Date/Time",                "DTM", false),
        ["DG1-6"]  = ("Diagnosis Type",                     "IS",  true),
        ["DG1-7"]  = ("Major Diagnostic Category",          "CNE", false),
        ["DG1-8"]  = ("Diagnostic Related Group",           "CNE", false),
        ["DG1-9"]  = ("DRG Approval Indicator",             "ID",  false),
        ["DG1-10"] = ("DRG Grouper Review Code",            "IS",  false),
        ["DG1-11"] = ("Outlier Type",                       "CNE", false),
        ["DG1-12"] = ("Outlier Days",                       "NM",  false),
        ["DG1-13"] = ("Outlier Cost",                       "CP",  false),
        ["DG1-14"] = ("Grouper Version and Type",           "ST",  false),
        ["DG1-15"] = ("Diagnosis Priority",                 "ID",  false),
        ["DG1-16"] = ("Diagnosing Clinician",               "XCN", false),
        ["DG1-17"] = ("Diagnosis Classification",           "IS",  false),
        ["DG1-18"] = ("Confidential Indicator",             "ID",  false),
        ["DG1-19"] = ("Attestation Date/Time",              "DTM", false),
        ["DG1-20"] = ("Diagnosis Identifier",               "EI",  false),
        ["DG1-21"] = ("Diagnosis Action Code",              "ID",  false),

        // ── SCH ──────────────────────────────────────────────────────────────
        ["SCH-1"]  = ("Placer Appointment ID",              "EI",  false),
        ["SCH-2"]  = ("Filler Appointment ID",              "EI",  false),
        ["SCH-3"]  = ("Occurrence Number",                  "NM",  false),
        ["SCH-4"]  = ("Placer Group Number",                "EI",  false),
        ["SCH-5"]  = ("Schedule ID",                        "CWE", false),
        ["SCH-6"]  = ("Event Reason",                       "CWE", true),
        ["SCH-7"]  = ("Appointment Reason",                 "CWE", false),
        ["SCH-8"]  = ("Appointment Type",                   "CWE", false),
        ["SCH-9"]  = ("Appointment Duration",               "NM",  false),
        ["SCH-10"] = ("Appointment Duration Units",         "CNE", false),
        ["SCH-11"] = ("Appointment Timing Quantity",        "TQ",  false),
        ["SCH-12"] = ("Placer Contact Person",              "XCN", false),
        ["SCH-13"] = ("Placer Contact Phone Number",        "XTN", false),
        ["SCH-14"] = ("Placer Contact Address",             "XAD", false),
        ["SCH-15"] = ("Placer Contact Location",            "PL",  false),
        ["SCH-16"] = ("Filler Contact Person",              "XCN", false),
        ["SCH-17"] = ("Filler Contact Phone Number",        "XTN", false),
        ["SCH-18"] = ("Filler Contact Address",             "XAD", false),
        ["SCH-19"] = ("Filler Contact Location",            "PL",  false),
        ["SCH-20"] = ("Entered By Person",                  "XCN", true),
        ["SCH-21"] = ("Entered By Phone Number",            "XTN", false),
        ["SCH-22"] = ("Entered By Location",                "PL",  false),
        ["SCH-23"] = ("Parent Placer Appointment ID",       "EI",  false),
        ["SCH-24"] = ("Parent Filler Appointment ID",       "EI",  false),
        ["SCH-25"] = ("Filler Status Code",                 "CWE", false),
        ["SCH-26"] = ("Placer Order Number",                "EI",  false),
        ["SCH-27"] = ("Filler Order Number",                "EI",  false),

        // ── RGS ──────────────────────────────────────────────────────────────
        ["RGS-1"]  = ("Set ID - RGS",                       "SI",  true),
        ["RGS-2"]  = ("Segment Action Code",                "ID",  false),
        ["RGS-3"]  = ("Resource Group ID",                  "CWE", false),

        // ── AIG ──────────────────────────────────────────────────────────────
        ["AIG-1"]  = ("Set ID - AIG",                       "SI",  true),
        ["AIG-2"]  = ("Segment Action Code",                "ID",  false),
        ["AIG-3"]  = ("Resource ID",                        "CWE", false),
        ["AIG-4"]  = ("Resource Type",                      "CWE", true),
        ["AIG-5"]  = ("Resource Group",                     "CWE", false),
        ["AIG-6"]  = ("Resource Quantity",                  "NM",  false),
        ["AIG-7"]  = ("Resource Quantity Units",            "CNE", false),
        ["AIG-8"]  = ("Start Date/Time",                    "DTM", false),
        ["AIG-9"]  = ("Start Date/Time Offset",             "NM",  false),
        ["AIG-10"] = ("Start Date/Time Offset Units",       "CNE", false),
        ["AIG-11"] = ("Duration",                           "NM",  false),
        ["AIG-12"] = ("Duration Units",                     "CNE", false),
        ["AIG-13"] = ("Allow Substitution Code",            "IS",  false),
        ["AIG-14"] = ("Filler Status Code",                 "CWE", false),

        // ── AIL ──────────────────────────────────────────────────────────────
        ["AIL-1"]  = ("Set ID - AIL",                       "SI",  true),
        ["AIL-2"]  = ("Segment Action Code",                "ID",  false),
        ["AIL-3"]  = ("Location Resource ID",               "PL",  false),
        ["AIL-4"]  = ("Location Type - AIL",                "CWE", false),
        ["AIL-5"]  = ("Location Group",                     "CWE", false),
        ["AIL-6"]  = ("Start Date/Time",                    "DTM", false),
        ["AIL-7"]  = ("Start Date/Time Offset",             "NM",  false),
        ["AIL-8"]  = ("Start Date/Time Offset Units",       "CNE", false),
        ["AIL-9"]  = ("Duration",                           "NM",  false),
        ["AIL-10"] = ("Duration Units",                     "CNE", false),
        ["AIL-11"] = ("Allow Substitution Code",            "IS",  false),
        ["AIL-12"] = ("Filler Status Code",                 "CWE", false),

        // ── AIP ──────────────────────────────────────────────────────────────
        ["AIP-1"]  = ("Set ID - AIP",                       "SI",  true),
        ["AIP-2"]  = ("Segment Action Code",                "ID",  false),
        ["AIP-3"]  = ("Personnel Resource ID",              "XCN", false),
        ["AIP-4"]  = ("Resource Type",                      "CWE", false),
        ["AIP-5"]  = ("Resource Group",                     "CWE", false),
        ["AIP-6"]  = ("Start Date/Time",                    "DTM", false),
        ["AIP-7"]  = ("Start Date/Time Offset",             "NM",  false),
        ["AIP-8"]  = ("Start Date/Time Offset Units",       "CNE", false),
        ["AIP-9"]  = ("Duration",                           "NM",  false),
        ["AIP-10"] = ("Duration Units",                     "CNE", false),
        ["AIP-11"] = ("Allow Substitution Code",            "IS",  false),
        ["AIP-12"] = ("Filler Status Code",                 "CWE", false),

        // ── AIS ──────────────────────────────────────────────────────────────
        ["AIS-1"]  = ("Set ID - AIS",                       "SI",  true),
        ["AIS-2"]  = ("Segment Action Code",                "ID",  false),
        ["AIS-3"]  = ("Universal Service Identifier",       "CWE", true),
        ["AIS-4"]  = ("Start Date/Time",                    "DTM", false),
        ["AIS-5"]  = ("Start Date/Time Offset",             "NM",  false),
        ["AIS-6"]  = ("Start Date/Time Offset Units",       "CNE", false),
        ["AIS-7"]  = ("Duration",                           "NM",  false),
        ["AIS-8"]  = ("Duration Units",                     "CNE", false),
        ["AIS-9"]  = ("Allow Substitution Code",            "IS",  false),
        ["AIS-10"] = ("Filler Status Code",                 "CWE", false),

        // ── SAC ──────────────────────────────────────────────────────────────
        ["SAC-1"]  = ("External Accession Identifier",      "EI",  false),
        ["SAC-2"]  = ("Accession Identifier",               "EI",  false),
        ["SAC-3"]  = ("Container Identifier",               "EI",  false),
        ["SAC-4"]  = ("Primary (Parent) Container Identifier","EI",false),
        ["SAC-5"]  = ("Equipment Container Identifier",     "EI",  false),
        ["SAC-6"]  = ("Specimen Source",                    "SPS", false),
        ["SAC-7"]  = ("Registration Date/Time",             "DTM", false),
        ["SAC-8"]  = ("Container Status",                   "CWE", false),
        ["SAC-9"]  = ("Carrier Type",                       "CWE", false),
        ["SAC-10"] = ("Carrier Identifier",                 "EI",  false),
        ["SAC-11"] = ("Position in Carrier",                "NA",  false),
        ["SAC-12"] = ("Tray Type - SAC",                    "CWE", false),
        ["SAC-13"] = ("Tray Identifier",                    "EI",  false),
        ["SAC-14"] = ("Position in Tray",                   "NA",  false),
        ["SAC-15"] = ("Location",                           "CWE", false),
        ["SAC-16"] = ("Container Height",                   "NM",  false),
        ["SAC-17"] = ("Container Diameter",                 "NM",  false),
        ["SAC-18"] = ("Barrier Delta",                      "NM",  false),
        ["SAC-19"] = ("Bottom Delta",                       "NM",  false),
        ["SAC-20"] = ("Container Height/Diameter/Delta Units","CWE",false),
        ["SAC-21"] = ("Container Volume",                   "NM",  false),
        ["SAC-22"] = ("Available Specimen Volume",          "NM",  false),
        ["SAC-23"] = ("Initial Specimen Volume",            "NM",  false),
        ["SAC-24"] = ("Volume Units",                       "CWE", false),
        ["SAC-25"] = ("Separator Type",                     "CWE", false),
        ["SAC-26"] = ("Cap Type",                           "CWE", false),
        ["SAC-27"] = ("Additive",                           "CWE", false),
        ["SAC-28"] = ("Specimen Component",                 "CWE", false),
        ["SAC-29"] = ("Dilution Factor",                    "SN",  false),
        ["SAC-30"] = ("Treatment",                          "CWE", false),
        ["SAC-31"] = ("Temperature",                        "SN",  false),
        ["SAC-32"] = ("Hemolysis Index",                    "NM",  false),
        ["SAC-33"] = ("Hemolysis Index Units",              "CWE", false),
        ["SAC-34"] = ("Lipemia Index",                      "NM",  false),
        ["SAC-35"] = ("Lipemia Index Units",                "CWE", false),
        ["SAC-36"] = ("Icterus Index",                      "NM",  false),
        ["SAC-37"] = ("Icterus Index Units",                "CWE", false),
        ["SAC-38"] = ("Fibrin Index",                       "NM",  false),
        ["SAC-39"] = ("Fibrin Index Units",                 "CWE", false),
        ["SAC-40"] = ("System Induced Contaminants",        "CWE", false),
        ["SAC-41"] = ("Drug Interference",                  "CWE", false),
        ["SAC-42"] = ("Artificial Blood",                   "CWE", false),
        ["SAC-43"] = ("Special Handling Code",              "CWE", false),
        ["SAC-44"] = ("Other Environmental Factors",        "CWE", false),
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Component names  (key = "DATATYPE.COMPONENTINDEX")
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string> ComponentNames
        = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── CWE : Coded with Exceptions ──────────────────────────────────────
        ["CWE.1"] = "Identifier",
        ["CWE.2"] = "Text",
        ["CWE.3"] = "Name of Coding System",
        ["CWE.4"] = "Alternate Identifier",
        ["CWE.5"] = "Alternate Text",
        ["CWE.6"] = "Name of Alternate Coding System",
        ["CWE.7"] = "Coding System Version ID",
        ["CWE.8"] = "Alternate Coding System Version ID",
        ["CWE.9"] = "Original Text",
        ["CWE.10"] = "Second Alternate Identifier",
        ["CWE.11"] = "Second Alternate Text",
        ["CWE.12"] = "Name of Second Alternate Coding System",
        ["CWE.13"] = "Second Alternate Coding System Version ID",
        ["CWE.14"] = "Coding System OID",
        ["CWE.15"] = "Value Set OID",
        ["CWE.16"] = "Value Set Version ID",
        ["CWE.17"] = "Alternate Coding System OID",
        ["CWE.18"] = "Alternate Value Set OID",
        ["CWE.19"] = "Alternate Value Set Version ID",
        ["CWE.20"] = "Second Alternate Coding System OID",
        ["CWE.21"] = "Second Alternate Value Set OID",
        ["CWE.22"] = "Second Alternate Value Set Version ID",

        // ── CNE : Coded with No Exceptions ───────────────────────────────────
        ["CNE.1"] = "Identifier",
        ["CNE.2"] = "Text",
        ["CNE.3"] = "Name of Coding System",
        ["CNE.4"] = "Alternate Identifier",
        ["CNE.5"] = "Alternate Text",
        ["CNE.6"] = "Name of Alternate Coding System",
        ["CNE.7"] = "Coding System Version ID",
        ["CNE.8"] = "Alternate Coding System Version ID",
        ["CNE.9"] = "Original Text",

        // ── CE : Coded Entry (older versions) ────────────────────────────────
        ["CE.1"] = "Identifier",
        ["CE.2"] = "Text",
        ["CE.3"] = "Name of Coding System",
        ["CE.4"] = "Alternate Identifier",
        ["CE.5"] = "Alternate Text",
        ["CE.6"] = "Name of Alternate Coding System",

        // ── CX : Extended Composite ID with Check Digit ──────────────────────
        ["CX.1"] = "ID Number",
        ["CX.2"] = "Identifier Check Digit",
        ["CX.3"] = "Check Digit Scheme",
        ["CX.4"] = "Assigning Authority",
        ["CX.5"] = "Identifier Type Code",
        ["CX.6"] = "Assigning Facility",
        ["CX.7"] = "Effective Date",
        ["CX.8"] = "Expiration Date",
        ["CX.9"] = "Assigning Jurisdiction",
        ["CX.10"] = "Assigning Agency or Department",
        ["CX.11"] = "Security Check",
        ["CX.12"] = "Security Check Scheme",

        // ── XPN : Extended Person Name ────────────────────────────────────────
        ["XPN.1"] = "Family Name",
        ["XPN.2"] = "Given Name",
        ["XPN.3"] = "Second and Further Given Names",
        ["XPN.4"] = "Suffix",
        ["XPN.5"] = "Prefix",
        ["XPN.6"] = "Degree",
        ["XPN.7"] = "Name Type Code",
        ["XPN.8"] = "Name Representation Code",
        ["XPN.9"] = "Name Context",
        ["XPN.10"] = "Name Validity Range",
        ["XPN.11"] = "Name Assembly Order",
        ["XPN.12"] = "Effective Date",
        ["XPN.13"] = "Expiration Date",
        ["XPN.14"] = "Professional Suffix",

        // ── XAD : Extended Address ────────────────────────────────────────────
        ["XAD.1"] = "Street Address",
        ["XAD.2"] = "Other Designation",
        ["XAD.3"] = "City",
        ["XAD.4"] = "State or Province",
        ["XAD.5"] = "Zip or Postal Code",
        ["XAD.6"] = "Country",
        ["XAD.7"] = "Address Type",
        ["XAD.8"] = "Other Geographic Designation",
        ["XAD.9"] = "County/Parish Code",
        ["XAD.10"] = "Census Tract",
        ["XAD.11"] = "Address Representation Code",
        ["XAD.12"] = "Address Validity Range",
        ["XAD.13"] = "Effective Date",
        ["XAD.14"] = "Expiration Date",
        ["XAD.15"] = "Expiration Reason",
        ["XAD.16"] = "Temporary Indicator",
        ["XAD.17"] = "Bad Address Indicator",
        ["XAD.18"] = "Address Usage",
        ["XAD.19"] = "Addressee",
        ["XAD.20"] = "Comment",
        ["XAD.21"] = "Preference Order",
        ["XAD.22"] = "Protection Code",
        ["XAD.23"] = "Address Identifier",

        // ── XTN : Extended Telecommunication Number ───────────────────────────
        ["XTN.1"] = "Telephone Number",
        ["XTN.2"] = "Telecommunication Use Code",
        ["XTN.3"] = "Telecommunication Equipment Type",
        ["XTN.4"] = "Communication Address",
        ["XTN.5"] = "Country Code",
        ["XTN.6"] = "Area/City Code",
        ["XTN.7"] = "Local Number",
        ["XTN.8"] = "Extension",
        ["XTN.9"] = "Any Text",
        ["XTN.10"] = "Extension Prefix",
        ["XTN.11"] = "Speed Dial Code",
        ["XTN.12"] = "Unformatted Telephone Number",
        ["XTN.13"] = "Effective Start Date",
        ["XTN.14"] = "Expiration Date",
        ["XTN.15"] = "Expiration Reason",
        ["XTN.16"] = "Protection Code",
        ["XTN.17"] = "Shared Telecommunication Identifier",
        ["XTN.18"] = "Preference Order",

        // ── XCN : Extended Composite ID Number and Name for Persons ──────────
        ["XCN.1"] = "Person Identifier",
        ["XCN.2"] = "Family Name",
        ["XCN.3"] = "Given Name",
        ["XCN.4"] = "Second and Further Given Names",
        ["XCN.5"] = "Suffix",
        ["XCN.6"] = "Prefix",
        ["XCN.7"] = "Degree",
        ["XCN.8"] = "Source Table",
        ["XCN.9"] = "Assigning Authority",
        ["XCN.10"] = "Name Type Code",
        ["XCN.11"] = "Identifier Check Digit",
        ["XCN.12"] = "Check Digit Scheme",
        ["XCN.13"] = "Identifier Type Code",
        ["XCN.14"] = "Assigning Facility",
        ["XCN.15"] = "Name Representation Code",
        ["XCN.16"] = "Name Context",
        ["XCN.17"] = "Name Validity Range",
        ["XCN.18"] = "Name Assembly Order",
        ["XCN.19"] = "Effective Date",
        ["XCN.20"] = "Expiration Date",
        ["XCN.21"] = "Professional Suffix",
        ["XCN.22"] = "Assigning Jurisdiction",
        ["XCN.23"] = "Assigning Agency or Department",

        // ── PL : Person Location ──────────────────────────────────────────────
        ["PL.1"] = "Point of Care",
        ["PL.2"] = "Room",
        ["PL.3"] = "Bed",
        ["PL.4"] = "Facility",
        ["PL.5"] = "Location Status",
        ["PL.6"] = "Person Location Type",
        ["PL.7"] = "Building",
        ["PL.8"] = "Floor",
        ["PL.9"] = "Location Description",
        ["PL.10"] = "Comprehensive Location Identifier",
        ["PL.11"] = "Assigning Authority for Location",

        // ── EI : Entity Identifier ────────────────────────────────────────────
        ["EI.1"] = "Entity Identifier",
        ["EI.2"] = "Namespace ID",
        ["EI.3"] = "Universal ID",
        ["EI.4"] = "Universal ID Type",

        // ── HD : Hierarchic Designator ────────────────────────────────────────
        ["HD.1"] = "Namespace ID",
        ["HD.2"] = "Universal ID",
        ["HD.3"] = "Universal ID Type",

        // ── MSG : Message Type ────────────────────────────────────────────────
        ["MSG.1"] = "Message Code",
        ["MSG.2"] = "Trigger Event",
        ["MSG.3"] = "Message Structure",

        // ── PT : Processing Type ──────────────────────────────────────────────
        ["PT.1"] = "Processing ID",
        ["PT.2"] = "Processing Mode",

        // ── VID : Version Identifier ──────────────────────────────────────────
        ["VID.1"] = "Version ID",
        ["VID.2"] = "Internationalization Code",
        ["VID.3"] = "International Version ID",

        // ── CQ : Composite Quantity with Units ────────────────────────────────
        ["CQ.1"] = "Quantity",
        ["CQ.2"] = "Units",

        // ── FC : Financial Class ──────────────────────────────────────────────
        ["FC.1"] = "Financial Class Code",
        ["FC.2"] = "Effective Date",

        // ── DLD : Discharge to Location and Date ─────────────────────────────
        ["DLD.1"] = "Discharge to Location",
        ["DLD.2"] = "Effective Date",

        // ── TQ : Timing Quantity (HL7 v2.3-v2.5) ─────────────────────────────
        ["TQ.1"] = "Quantity",
        ["TQ.2"] = "Interval",
        ["TQ.3"] = "Duration",
        ["TQ.4"] = "Start Date/Time",
        ["TQ.5"] = "End Date/Time",
        ["TQ.6"] = "Priority",
        ["TQ.7"] = "Condition",
        ["TQ.8"] = "Text",
        ["TQ.9"] = "Conjunction",
        ["TQ.10"] = "Order Sequencing",
        ["TQ.11"] = "Occurrence Duration",
        ["TQ.12"] = "Total Occurrences",

        // ── EIP : Entity Identifier Pair ─────────────────────────────────────
        ["EIP.1"] = "Placer Assigned Identifier",
        ["EIP.2"] = "Filler Assigned Identifier",

        // ── SN : Structured Numeric ───────────────────────────────────────────
        ["SN.1"] = "Comparator",
        ["SN.2"] = "Num1",
        ["SN.3"] = "Separator/Suffix",
        ["SN.4"] = "Num2",

        // ── TS : Time Stamp (older HL7 versions) ─────────────────────────────
        ["TS.1"] = "Time",
        ["TS.2"] = "Degree of Precision",

        // ── CP : Composite Price ──────────────────────────────────────────────
        ["CP.1"] = "Price",
        ["CP.2"] = "Price Type",
        ["CP.3"] = "From Value",
        ["CP.4"] = "To Value",
        ["CP.5"] = "Range Units",
        ["CP.6"] = "Range Type",

        // ── MOC : Money and Charge Code ───────────────────────────────────────
        ["MOC.1"] = "Monetary Amount",
        ["MOC.2"] = "Charge Code",

        // ── XON : Extended Composite Name and Identification Number for Organizations
        ["XON.1"] = "Organization Name",
        ["XON.2"] = "Organization Name Type Code",
        ["XON.3"] = "ID Number",
        ["XON.4"] = "Check Digit",
        ["XON.5"] = "Check Digit Scheme",
        ["XON.6"] = "Assigning Authority",
        ["XON.7"] = "Identifier Type Code",
        ["XON.8"] = "Assigning Facility",
        ["XON.9"] = "Name Representation Code",
        ["XON.10"] = "Organization Identifier",

        // ── JCC : Job Code/Class ──────────────────────────────────────────────
        ["JCC.1"] = "Job Code",
        ["JCC.2"] = "Job Class",
        ["JCC.3"] = "Job Description Text",

        // ── NA : Numeric Array ────────────────────────────────────────────────
        ["NA.1"] = "Value1",
        ["NA.2"] = "Value2",
        ["NA.3"] = "Value3",
        ["NA.4"] = "Value4",

        // ── DLN : Driver's License Number ────────────────────────────────────
        ["DLN.1"] = "License Number",
        ["DLN.2"] = "Issuing State/Province/Country",
        ["DLN.3"] = "Expiration Date",

        // ── NDL : Name with Date and Location ────────────────────────────────
        ["NDL.1"] = "Name",
        ["NDL.2"] = "Start Date/Time",
        ["NDL.3"] = "End Date/Time",
        ["NDL.4"] = "Point of Care",
        ["NDL.5"] = "Room",
        ["NDL.6"] = "Bed",
        ["NDL.7"] = "Facility",
        ["NDL.8"] = "Location Status",
        ["NDL.9"] = "Patient Location Type",
        ["NDL.10"] = "Building",
        ["NDL.11"] = "Floor",

        // ── PRL : Parent Result Link ──────────────────────────────────────────
        ["PRL.1"] = "Parent Observation Identifier",
        ["PRL.2"] = "Parent Observation Sub-Identifier",
        ["PRL.3"] = "Parent Observation Value Descriptor",

        // ── SPS : Specimen Source ─────────────────────────────────────────────
        ["SPS.1"] = "Specimen Source Name or Code",
        ["SPS.2"] = "Additives",
        ["SPS.3"] = "Specimen Collection Method",
        ["SPS.4"] = "Body Site",
        ["SPS.5"] = "Site Modifier",
        ["SPS.6"] = "Collection Method Modifier Code",
        ["SPS.7"] = "Specimen Role",

        // ── FN : Family Name ──────────────────────────────────────────────────
        ["FN.1"] = "Surname",
        ["FN.2"] = "Own Surname Prefix",
        ["FN.3"] = "Own Surname",
        ["FN.4"] = "Surname Prefix from Partner/Spouse",
        ["FN.5"] = "Surname from Partner/Spouse",

        // ── RI : Repeat Interval ─────────────────────────────────────────────
        ["RI.1"] = "Repeat Pattern",
        ["RI.2"] = "Explicit Time Interval",

        // ── ERL : Error Location ──────────────────────────────────────────────
        ["ERL.1"] = "Segment ID",
        ["ERL.2"] = "Segment Sequence",
        ["ERL.3"] = "Field Position",
        ["ERL.4"] = "Field Repetition",
        ["ERL.5"] = "Component Number",
        ["ERL.6"] = "Sub-Component Number",
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the human-readable segment name, or the segment ID if unknown.</summary>
    public static string GetSegmentName(string segmentId)
    {
        if (string.IsNullOrEmpty(segmentId)) return string.Empty;
        return SegmentNames.TryGetValue(segmentId, out var name) ? name : string.Empty;
    }

    /// <summary>Returns the field name for a given segment + 1-based field index.</summary>
    public static string GetFieldName(string segmentId, int fieldIndex)
    {
        var key = $"{segmentId}-{fieldIndex}";
        return FieldInfo.TryGetValue(key, out var info) ? info.Name : string.Empty;
    }

    /// <summary>Returns the HL7 data type code for a given segment + 1-based field index.</summary>
    public static string GetFieldDataType(string segmentId, int fieldIndex)
    {
        var key = $"{segmentId}-{fieldIndex}";
        return FieldInfo.TryGetValue(key, out var info) ? info.DataType : string.Empty;
    }

    /// <summary>Returns true if the field is required per the HL7 specification.</summary>
    public static bool IsFieldRequired(string segmentId, int fieldIndex)
    {
        var key = $"{segmentId}-{fieldIndex}";
        return FieldInfo.TryGetValue(key, out var info) && info.Required;
    }

    /// <summary>
    /// Returns the component name for a given data type + 1-based component index.
    /// E.g. GetComponentName("PL", 1) → "Point of Care"
    /// </summary>
    public static string GetComponentName(string dataType, int componentIndex)
    {
        if (string.IsNullOrEmpty(dataType)) return string.Empty;
        var key = $"{dataType}.{componentIndex}";
        return ComponentNames.TryGetValue(key, out var name) ? name : string.Empty;
    }

    /// <summary>
    /// Returns a complete field info tuple for a given segment + field index.
    /// Returns null if the field is not in the knowledge base.
    /// </summary>
    public static (string Name, string DataType, bool Required)? TryGetFieldInfo(string segmentId, int fieldIndex)
    {
        var key = $"{segmentId}-{fieldIndex}";
        return FieldInfo.TryGetValue(key, out var info) ? info : null;
    }
}
