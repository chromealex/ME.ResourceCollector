# ME.ResourceCollector

## Install

### Using submodule

Add as a submodule this repository https://github.com/chromealex/ME.ResourceCollector and choose target directory ```Assets/ME.ResourceCollector```.

### Using Unity Package Manager

1. Open Packages/manifest.json file.
2. Add ME.ECS to your dependencies section:
```
{
  "dependencies": {
    [HERE ARE OTHER PACKAGES]
    "com.me.resourcecollector": "https://github.com/chromealex/ME.ResourceCollector.git"
  }
}
```

## Run

Choose ```Tools/ME.ResourceCollector/Update Resources``` menu to collect assets data.<br>
If you want to recalculate all asset sizes, choose ```Tools/ME.ResourceCollector/Recalculate Resource Sizes``` menu.
