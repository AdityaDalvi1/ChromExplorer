#################################################################
#
# Patch all of the given *.rc files and set
# the version strings for DLLs and OCX controls.
#
#################################################################


# Hand parse the arguments so we can separate them with spaces.
$files = @()

$previousArg = "__" 
foreach ($arg in $args)
{
    if ($previousArg -eq "-version")
    {
        $version = $arg
    }
    elseif ($previousArg -eq "__")
    {
    }
    else
    {
        $files += $arg
    }
    $previousArg = $arg
}

Function PatchRCFiles([string]$version, [string[]]$files)
{
        echo "Patching all .rc files to version $version"

        # patch the files we found
        ForEach ($file In $files) 
        { 
            echo "Processing $file..."
            $content = (Get-Content $file)
            $content | 
            Foreach-Object {
                $_ -replace "!define PRODUCT_VERSION.+", "!define PRODUCT_VERSION `"$version`"" 
            }  | 
            Set-Content $file
          }
    
}

PatchRCFiles $version $files

