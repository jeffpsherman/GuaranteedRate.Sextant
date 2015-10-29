﻿using EllieMae.Encompass.BusinessObjects.Loans;
using EllieMae.Encompass.BusinessObjects.Loans.Logging;
using EllieMae.Encompass.Client;
using GuaranteedRate.Sextant.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuaranteedRate.Sextant.EncompassUtils
{
    /**
     * Encompass has a LoanUtils class
     */
    public class LoanDataUtils
    {
        public const int MULTI_MAX = 10;

        /**
         * Still a work in progress - ideally this function will iterate 
         * through a loan's fields and return a Dictonary representation.
         * 
         * Works fine with SimpleFields, but not most types of multi-field values
         */
        public static IDictionary<string, object> ExtractLoanFields(Loan currentLoan)
        {
            IDictionary<string, object> fieldValues = new Dictionary<string, object>();

            ExtractSimpleFields(currentLoan, FieldUtils.SimpleFieldNames(), fieldValues);
            ExtractMiddleIndexFields(currentLoan, FieldUtils.MiddleIndexMulti(), fieldValues);
            ExtractEndIndexFields(currentLoan, FieldUtils.EndIndexMulti(), fieldValues);
            ExtractStringIndexFields(currentLoan, FieldUtils.DocumentMulti(), GetDocumentIndexes(currentLoan), fieldValues);
            ExtractStringIndexFields(currentLoan, FieldUtils.PostClosingMulti(), GetPostClosingIndexes(currentLoan), fieldValues);
            ExtractStringIndexFields(currentLoan, FieldUtils.UnderwritingMulti(), GetUnderwritingIndexes(currentLoan), fieldValues);
            ExtractStringIndexFields(currentLoan, FieldUtils.MilestoneTaskMulti(), GetMilestoneTaskIndexes(currentLoan), fieldValues);

            int borrowerEmployerCount = currentLoan.BorrowerEmployers.Count;
            int coBorrowerEmployerCount = currentLoan.CoBorrowerEmployers.Count;

            int liabilities = currentLoan.Liabilities.Count;
            int mortgages = currentLoan.Mortgages.Count;
            

            //This is a subset of the borrower pair information, there does not seem to be an efficient method for
            //extracting all of this data programmatically.
            fieldValues.Add("borrower-pairs", ExtractBorrowerPairs(currentLoan));
            fieldValues.Add("Associates", ExtractAssociates(currentLoan));

            ExtractProperties(currentLoan, fieldValues);

            return fieldValues;
        }

        public static IDictionary<string, object> ExtractEverything(Loan loan)
        {
            IDictionary<string, object> loanData = new Dictionary<string, object>();
            if (loan != null)
            {
                try
                {
                    var lastModified = loan.LastModified;
                    if (lastModified != null)
                    {
                        loanData.Add("lastmodified", loan.LastModified.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Loggly.Error("LoandataUtils", "Exception in ExtractEverything while getting LastModified:" + ex);
                }
                try
                {
                    loanData.Add("fields", ExtractLoanFields(loan));
                }
                catch (Exception ex)
                {
                    Loggly.Error("LoandataUtils", "Exception in ExtractEverything while getting fields:" + ex);
                }
                try
                {
                    loanData.Add("milestones", ExtractMilestones(loan));
                }
                catch (Exception ex)
                {
                    Loggly.Error("LoandataUtils", "Exception in ExtractEverything while getting Milestones:" + ex);
                }
            }
            return loanData;
        }

        /**
         * This method is for common useful 'metadata' information that is not part of the loan data itself
         * Such as the LoanFolder, UserId doing the extraction, etc
         */
        public static IDictionary<string, object> ExtractProperties(Loan loan, IDictionary<string, object> fieldValues)
        {
            try
            {
                fieldValues.Add("LoanFolder", loan.LoanFolder);
                Session session = loan.Session;
                if (session != null) 
                {
                    fieldValues.Add("SessionUserId", session.UserID);
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractProperties:" + ex);
            }
            return fieldValues;
        }

        public static IList<IDictionary<string, string>> ExtractAssociates(Loan loan)
        {
            IList<IDictionary<string, string>> associateExtract = new List<IDictionary<string, string>>();
            try
            {
                LoanAssociates associates = loan.Associates;
                foreach (LoanAssociate associate in associates)
                {
                    IDictionary<string, string> extract = new Dictionary<string, string>();
                    if (associate.User != null)
                    {
                        extract.Add("FullName", associate.User.FullName);
                    }
                    if (associate.WorkflowRole != null)
                    {
                        extract.Add("WorkflowRole", associate.WorkflowRole.Name);
                    }
                    if (associate.MilestoneEvent != null)
                    {
                        extract.Add("MilestoneEvent", associate.MilestoneEvent.MilestoneName);
                    }
                    if (associate.UserGroup != null)
                    {
                        extract.Add("UserGroup", associate.UserGroup.Name);
                    }
                    extract.Add("Assigned", associate.Assigned + "");
                    extract.Add("AllowWriteAccess", associate.AllowWriteAccess + "");
                    extract.Add("AssociateType", associate.AssociateType + "");
                    extract.Add("ContactCellPhone", associate.ContactCellPhone);
                    extract.Add("ContactEmail", associate.ContactEmail);
                    extract.Add("ContactFax", associate.ContactFax);
                    extract.Add("ContactName", associate.ContactName);
                    extract.Add("ContactPhone", associate.ContactPhone);
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractAssociates:" + ex);
            }
            return associateExtract;
        }

        /**
         * There's no specific list of fields affected by borrower pairs.
         * We've defined a set that's useful to us, but you can override with your own
         */ 
        public static IList<IDictionary<string, object>> ExtractBorrowerPairs(Loan loan)
        {
            return ExtractBorrowerPairs(loan, FieldUtils.BORROWER_PAIR_FIELDS);
        }

        public static IList<IDictionary<string, object>> ExtractBorrowerPairs(Loan loan, IList<string> fields)
        {
            IList<IDictionary<string, object>> borrowerPairs = new List<IDictionary<string, object>>();
            try
            {
                string primarySsn = FormatSSN(ExtractSimpleField(loan, "65"));
                foreach (BorrowerPair pair in loan.BorrowerPairs)
                {
                    IDictionary<string, object> fieldDictionary = new Dictionary<string, object>();
                    borrowerPairs.Add(ExtractSimpleFields(loan, pair, fields, fieldDictionary));
                    string ssn = FormatSSN(ExtractSimpleField(loan, "65"));
                    if (ssn != null && ssn == primarySsn)
                    {
                        fieldDictionary.Add("PrimaryPair", true);
                    }
                    else
                    {
                        fieldDictionary.Add("PrimaryPair", false);
                    }
                }
            }
            catch
            {
                //No-op - if there are no SSN this will throw an exception.
                //But this is a no-op because SSN is not required
            }
            return borrowerPairs;
        }

        public static IList<IDictionary<string, string>> ExtractMilestones(Loan loan)
        {
            IList<IDictionary<string, string>> milestones = new List<IDictionary<string, string>>();
            try
            {
                foreach (MilestoneEvent milestone in loan.Log.MilestoneEvents)
                {
                    IDictionary<string, string> localMilestone = new Dictionary<string, string>();
                    localMilestone.Add("milestoneName", ParseField(milestone.MilestoneName));
                    localMilestone.Add("completed", milestone.Completed.ToString());
                    localMilestone.Add("completedDate", ParseField(milestone.Date.ToString()));
                    string comments = ParseField(milestone.Comments);
                    if (!String.IsNullOrWhiteSpace(comments))
                    {
                        localMilestone.Add("comments", comments);
                    }
                    if ((milestone.LoanAssociate != null) && (milestone.LoanAssociate.User != null))
                    {
                        localMilestone.Add("userId", ParseField(milestone.LoanAssociate.User.ID));
                    }
                    milestones.Add(localMilestone);
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractMilestones:" + ex);
            }
            return milestones;
        }

        private static string FormatSSN(string ssn)
        {
            try
            {
                if (ssn != null)
                {
                    if (ssn.Length == 9)
                    {
                        return ssn.Insert(5, "-").Insert(3, "-");
                    }
                    return ssn;
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in FormatSSN:" + ex);
            }
            return null;
        }

        public static IList<string> GetDocumentIndexes(Loan currentLoan)
        {
            IList<string> keys = new List<string>();
            foreach (TrackedDocument document in currentLoan.Log.TrackedDocuments)
            {
                keys.Add(document.Title);
            }
            return keys;
        }

        public static IList<string> GetUnderwritingIndexes(Loan currentLoan)
        {
            IList<string> keys = new List<string>();
            foreach (UnderwritingCondition cond in currentLoan.Log.UnderwritingConditions)
            {
                keys.Add(cond.Title);
            }
            return keys;
        }

        public static IList<string> GetPostClosingIndexes(Loan currentLoan)
        {
            IList<string> keys = new List<string>();
            
            foreach (UnderwritingCondition cond in currentLoan.Log.PostClosingConditions)
            {
                keys.Add(cond.Title);
            }
            return keys;
        }

        public static IList<string> GetMilestoneTaskIndexes(Loan currentLoan)
        {
            IList<string> keys = new List<string>();
            foreach (MilestoneTask task in currentLoan.Log.MilestoneTasks)
            {
                keys.Add(task.Name);
            }
            return keys;
        }

        public static IDictionary<string, object> ExtractStringIndexFields(Loan currentLoan, IList<string> fieldIds, IList<string> keys, IDictionary<string, object> fieldDictionary)
        {
            if (keys == null || keys.Count == 0)
            {
                return fieldDictionary;
            }
            try
            {
                foreach (string fieldId in fieldIds)
                {
                    foreach (string key in keys)
                    {
                        string fullKey = fieldId + "." + key;
                        string val = ExtractSimpleField(currentLoan, fullKey);
                        if (val != null)
                        {
                            fieldDictionary.Add(SafeFieldId(fullKey), val);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractDocumentIndexFields:" + ex);
            }
            return fieldDictionary;
        }

        public static IDictionary<string, object> ExtractEndIndexFields(Loan currentLoan, IList<string> fieldIds, IDictionary<string, object> fieldDictionary)
        {
            try
            {
                foreach (string fieldId in fieldIds)
                {
                    int index = 0;
                    try
                    {
                        for (index = 1; index < MULTI_MAX; index++)
                        {
                            string fieldIdIndex = fieldId + "." + IntPad(index);
                            object fieldObject = currentLoan.Fields[fieldIdIndex].Value;
                            string value = ParseField(fieldObject);
                            if (value != null)
                            {
                                fieldDictionary.Add(SafeFieldId(fieldIdIndex), value);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //Debug.WriteLine("Failed to pull: " + fieldId + " index=" + index + " Exception: " + e);
                        Loggly.Error("LoandataUtils", "Failed to pull: " + fieldId + " index=" + index + " Exception: " + e);
                    }
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractEndIndexFields:" + ex);
            }
            return fieldDictionary;
        }

        private static string IntPad(int x)
        {
            if (x < 10)
            {
                return "0" + x;
            }
            else
            {
                return x + "";
            }
        }

        public static IDictionary<string, object> ExtractMiddleIndexFields(Loan currentLoan, IList<string> fieldIds, IDictionary<string, object> fieldDictionary)
        {
            try
            {
                foreach (string fieldId in fieldIds)
                {
                    int index = 0;
                    try
                    {
                        int offset = fieldId.IndexOf("00");
                        string pre = fieldId.Substring(0, offset);
                        string post = fieldId.Substring(offset + 2);

                        //Requesting 00 SHOULD always return null.  
                        for (index = 0; index < MULTI_MAX; index++)
                        {
                            string indexPad = pre + IntPad(index) + post;
                            object fieldObject = currentLoan.Fields[indexPad].Value;
                            string value = ParseField(fieldObject);
                            if (value != null)
                            {
                                fieldDictionary.Add(SafeFieldId(indexPad), value);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //Debug.WriteLine("Failed to pull: " + fieldId + " index=" + index + " Exception: " + e);
                        Loggly.Error("LoandataUtils", "Failed to pull: " + fieldId + " index=" + index + " Exception: " + e);
                    }
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractMiddleIndexFields:" + ex);
            }
            return fieldDictionary;
        }

        public static IDictionary<string, object> ExtractSimpleFields(Loan currentLoan, IList<string> fieldIds, IDictionary<string, object> fieldDictionary)
        {
            try
            {
                foreach (string fieldId in fieldIds)
                {
                    try
                    {
                        object fieldObject;
                        fieldObject = currentLoan.Fields[fieldId].Value;
                        string value = ParseField(fieldObject);
                        if (value != null)
                        {
                            fieldDictionary.Add(SafeFieldId(fieldId), value);
                        }
                    }
                    catch (Exception e)
                    {
                        //Debug.WriteLine("Failed to pull: " + fieldId + " Exception: " + e);
                        Loggly.Error("LoandataUtils", "Failed to pull: " + fieldId + " Exception: " + e);
                    }
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractSimpleFields:" + ex);
            }
            return fieldDictionary;
        }

        public static string ExtractSimpleField(Loan currentLoan, string field)
        {
            try
            {
                object fieldObject;
                fieldObject = currentLoan.Fields[field].Value;
                return ParseField(fieldObject);
            }
            catch (Exception e)
            {
                //Debug.WriteLine("Failed to pull: " + fieldId + " Exception: " + e);
                Loggly.Error("LoandataUtils", "Exception trying to access: " + field + " Exception: " + e);
            }
            return null;
        }

        public static IDictionary<string, object> ExtractSimpleFields(Loan currentLoan, BorrowerPair borrowerPair, IList<string> fieldIds, IDictionary<string, object> fieldDictionary)
        {
            try
            {
                foreach (string fieldId in fieldIds)
                {
                    try
                    {
                        object fieldObject;
                        fieldObject = currentLoan.Fields[fieldId].GetValueForBorrowerPair(borrowerPair);
                        string value = ParseField(fieldObject);
                        if (value != null)
                        {
                            fieldDictionary.Add(SafeFieldId(fieldId), value);
                        }
                    }
                    catch (Exception e)
                    {
                        //Debug.WriteLine("Failed to pull: " + fieldId + " Exception: " + e);
                        Loggly.Error("LoandataUtils", "Failed to pull: " + fieldId + " Exception: " + e);
                    }
                }
            }
            catch (Exception ex)
            {
                Loggly.Error("LoandataUtils", "Exception in ExtractSimpleFields with BorrowerPairs:" + ex);
            }
            return fieldDictionary;
        }

        public static string ParseField(object fieldObject)
        {
            if (fieldObject != null)
            {
                string fieldValue = fieldObject.ToString();
                if (!String.IsNullOrWhiteSpace(fieldValue))
                {
                    return fieldValue;
                }
            }
            return null;
        }

        /**
         * Some characters cause problems downstream and
         * are really just huge problems.
         * 
         * Removing spaces from field names
         */
        public static string SafeFieldId(string fieldId)
        {
            if (String.IsNullOrEmpty(fieldId))
            {
                return fieldId;
            }
            return fieldId.Replace(' ', '_');
        }
    }
}