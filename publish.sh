target='linux-x64'
# target='win-x64'
# target='osx-x64'

tag=$(git describe --tags --abbrev=0)
tag_no_v=$(echo $tag | sed 's/v//g')
release_name="cli-$tag-$target"

# Build everything
dotnet publish src/AzAcme.Cli/AzAcme.Cli.csproj -p:PublishSingleFile=true --runtime "$target" -c Release -o "$release_name" --self-contained true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=true /p:Version="$tag_no_v"

# Pack files
if [ "$target" == "win-x64" ]; then
  # Pack to zip for Windows
  7z a -tzip "./builds/${release_name}.zip" "./${release_name}/*"
else
tar czvf "./builds/${release_name}.tar.gz" "./$release_name"
fi

# Delete output directory
rm -r "$release_name"