using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace SecurityTests
{
    [TestFixture]
    public class DataRecoveryTests
    {
        [Test]
        public void FindEncryptedJournalFiles()
        {
            try 
            {
                // Check application data locations more thoroughly
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                // Search recursive for any encrypted-looking files
                var searchLocations = new[]
                {
                    homeDir,
                    Path.Combine(homeDir, ".config"),
                    Path.Combine(homeDir, ".local"),
                    Path.Combine(homeDir, "Library"),
                    Path.Combine(homeDir, "Documents"),
                    "/Users/bbm2/Documents/GitHub/Sophic",
                    "/Users/bbm2/Documents/GitHub/Sophic/bin/Debug/net8.0"
                };

                foreach (var location in searchLocations)
                {
                    if (Directory.Exists(location))
                    {
                        SearchForDataFiles(location);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
            }
        }
        
        private void SearchForDataFiles(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file).ToLower();
                    var content = string.Empty;
                    
                    try
                    {
                        content = File.ReadAllText(file);
                    }
                    catch
                    {
                        continue; // Skip files we can't read
                    }
                    
                    // Look for potential encrypted journal files
                    if ((fileName.Contains("journal") || fileName.Contains("sophic") || 
                         fileName.Contains("entries") || fileName.Contains("data")) &&
                        content.Length > 100)
                    {
                        Console.WriteLine($"POTENTIAL JOURNAL FILE: {file}");
                        Console.WriteLine($"  Size: {content.Length} chars");
                        Console.WriteLine($"  Preview: {content.Substring(0, Math.Min(200, content.Length))}");
                        
                        if (LooksLikeEncryptedData(content))
                        {
                            Console.WriteLine($"  ** ENCRYPTED DATA DETECTED **");
                        }
                        Console.WriteLine();
                    }
                    
                    // Also check for any base64-like content that could be encrypted journal data
                    if (content.Length > 500 && LooksLikeEncryptedData(content.Trim()))
                    {
                        Console.WriteLine($"ENCRYPTED FILE FOUND: {file}");
                        Console.WriteLine($"  Size: {content.Length} chars");
                        Console.WriteLine($"  Content: {content.Substring(0, Math.Min(100, content.Length))}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching {directory}: {ex.Message}");
            }
        }
        
        private bool LooksLikeEncryptedData(string content)
        {
            // Check if it's base64-like and substantial length
            return content.Length > 100 &&
                   content.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=' || char.IsWhiteSpace(c)) &&
                   content.Count(c => c == '=') <= 2; // Base64 padding
        }
    }
}