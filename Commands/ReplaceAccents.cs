using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using PlayfulSparkle;

namespace VsPsReplaceAccents
{
    /// <summary>
    /// A command handler for replacing accented characters in the active document or selected text.
    /// </summary>
    [Command(PackageIds.VsReplaceAccents)]
    internal sealed class ReplaceAccents : BaseCommand<ReplaceAccents>
    {
        /// <summary>
        /// Executes the command to replace accented characters.
        /// </summary>
        /// <param name="e">The OleMenuCmdEventArgs instance containing event data.</param>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

            if (docView == null)
            {
                return;
            }

            // Get settings
            ReplaceAccentsSettings settings = await ReplaceAccentsSettings.GetLiveInstanceAsync();

            // Convert the special character mappings to a dictionary for easier lookup.
            Dictionary<string, string> charMappings = settings.SpecialCharacterMappings.ToDictionary();

            bool wasModified = false;

            // Start the 'find' animation on the status bar to indicate processing.
            await VS.StatusBar.StartAnimationAsync(StatusAnimation.Find);

            // Get all selections from multi-cursor.
            NormalizedSnapshotSpanCollection selections = docView.TextView.Selection.SelectedSpans;

            // If there are non-empty selections, process them.
            if (selections.Count > 0 && !selections.All(span => span.IsEmpty))
            {
                wasModified = await ProcessSelectionAsync(docView, selections, settings.UseDefaultMappings, charMappings);
            }
            // Otherwise, process the entire document.
            else
            {
                wasModified = await ProcessDocumentAsync(docView, settings.UseDefaultMappings, charMappings);
            }

            // Update the status bar based on whether any changes were made.
            if (wasModified)
            {
                await VS.MessageBox.ShowAsync("Replace Accents", "Accented characters successfully replaced.", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }
            else
            {
                await VS.MessageBox.ShowAsync("Replace Accents", "No accented characters found or replaced.", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }

            // Stop the status bar animation.
            await VS.StatusBar.EndAnimationAsync(StatusAnimation.Find);
        }

        /// <summary>
        /// Processes the entire document to replace accented characters.
        /// </summary>
        /// <param name="docView">The active document view.</param>
        /// <param name="useDefaultMappings">Indicates whether to use default character mappings.</param>
        /// <param name="charMappings">A dictionary of custom character mappings.</param>
        /// <returns>True if the document was modified; otherwise, false.</returns>
        private async Task<bool> ProcessDocumentAsync(DocumentView docView, bool useDefaultMappings, Dictionary<string, string> charMappings)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            bool wasModified = false;

            IVsThreadedWaitDialogFactory factory = await VS.GetServiceAsync<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>();

            IVsThreadedWaitDialog2 dialog = null;

            factory.CreateInstance(out dialog);

            // Process the entire document line by line.
            using (ITextEdit edit = docView.TextBuffer.CreateEdit())
            {
                ITextSnapshot snapshot = docView.TextBuffer.CurrentSnapshot;

                int totalLines = snapshot.LineCount;
                string waitCaption = "Replacing Accents";
                string waitMessage = "Processing document lines...";

                // Start the progress dialog
                if (dialog != null)
                {
                    dialog.StartWaitDialogWithPercentageProgress(
                        szWaitCaption: waitCaption,
                        szWaitMessage: waitMessage,
                        szProgressText: null,
                        varStatusBmpAnim: null,
                        szStatusBarText: null,
                        fIsCancelable: false,
                        iDelayToShowDialog: 0,
                        iTotalSteps: totalLines,
                        iCurrentStep: 0
                    );
                }

                for (int idx = 0; idx < snapshot.LineCount; idx++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(idx);

                    string lineText = line.GetText();

                    string processedText = lineText;

                    // Skip processing for empty or whitespace-only lines.
                    if (string.IsNullOrWhiteSpace(lineText))
                    {
                        continue;
                    }

                    try
                    {
                        // Decompose the line text to replace accented characters.
                        processedText = await Transliterate.DecomposeAsync(lineText, Transliterate.Normalization.Decompose, useDefaultMappings, charMappings);
                    }
                    catch (Exception exc)
                    {
                        // Display an error message on the status bar if an exception occurs during processing.
                        await VS.StatusBar.ShowMessageAsync($"Error during accent replacement: {exc.Message}.");
                    }

                    // Only replace the line if the processed text is different from the original.
                    if (lineText != processedText)
                    {
                        edit.Replace(line.Start, line.Length, processedText);

                        wasModified = true;

                        if (dialog != null)
                        {
                            dialog.UpdateProgress(
                                szUpdatedWaitMessage: waitMessage,
                                szProgressText: $"Processed {idx + 1} of {totalLines} lines",
                                szStatusBarText: null,
                                iCurrentStep: idx + 1,
                                iTotalSteps: totalLines,
                                fDisableCancel: true,
                                pfCanceled: out bool canceled
                            );
                        }
                    }
                }

                // Apply the changes to the document if any modifications were made.
                if (wasModified)
                {
                    edit.Apply();
                }
            }

            if (dialog != null)
            {
                dialog.EndWaitDialog(out _);
            }

            return wasModified;
        }

        /// <summary>
        /// Processes the selected text ranges to replace accented characters.
        /// </summary>
        /// <param name="docView">The active document view.</param>
        /// <param name="selections">A collection of selected text spans.</param>
        /// <param name="useDefaultMappingss">Indicates whether to use default character mappings.</param>
        /// <param name="charMappings">A dictionary of custom character mappings.</param>
        /// <returns>True if any selection was modified; otherwise, false.</returns>
        internal static async Task<bool> ProcessSelectionAsync(
            DocumentView docView,
            NormalizedSnapshotSpanCollection selections,
            bool useDefaultMappingss,
            Dictionary<string, string> charMappings
        )
        {
            bool wasModified = false;

            // Process all non-empty selections in reverse order to avoid issues with span invalidation.
            IOrderedEnumerable<SnapshotSpan> reversedSelections = selections.Where(span => !span.IsEmpty).OrderByDescending(span => span.Start.Position);

            foreach (SnapshotSpan selection in reversedSelections)
            {
                string lineText = selection.GetText();

                string processedText = lineText;

                // Skip processing for empty or whitespace-only selections.
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    continue;
                }

                try
                {
                    // Decompose the selected text to replace accented characters.
                    processedText = await Transliterate.DecomposeAsync(lineText, Transliterate.Normalization.Decompose, useDefaultMappingss, charMappings);
                }
                catch (Exception exc)
                {
                    // Display an error message on the status bar if an exception occurs during processing.
                    await VS.StatusBar.ShowMessageAsync($"Error during accent replacement in selection: {exc.Message}.");
                }

                // Only replace the selected text if the processed text is different from the original.
                if (lineText != processedText)
                {
                    docView.TextBuffer.Replace(selection, processedText);

                    wasModified = true;
                }
            }

            return wasModified;
        }
    }
}