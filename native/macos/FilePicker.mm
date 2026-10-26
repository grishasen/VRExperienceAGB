#import <AppKit/AppKit.h>
#include <cstdlib>
#include <cstring>

extern "C" __attribute__((visibility("default"))) char *VRAGBPickFile(int profiles)
{
    __block char *result = nullptr;
    void (^choose)(void) = ^{
        @autoreleasepool {
            NSOpenPanel *panel = [NSOpenPanel openPanel];
            panel.title = profiles ? @"Import profiles or package" : @"Import model or package";
            panel.canChooseFiles = YES;
            panel.canChooseDirectories = NO;
            panel.allowsMultipleSelection = NO;
            panel.allowedFileTypes = profiles ? @[@"json", @"jsonl", @"zip"] : @[@"json", @"zip"];
            if ([panel runModal] == NSModalResponseOK)
                result = strdup(panel.URL.path.UTF8String);
        }
    };
    if ([NSThread isMainThread]) choose();
    else dispatch_sync(dispatch_get_main_queue(), choose);
    return result;
}

extern "C" __attribute__((visibility("default"))) void VRAGBFreePath(char *path)
{
    free(path);
}
