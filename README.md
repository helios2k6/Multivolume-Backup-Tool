Multivolume-Backup-Tool
=======================
An archiving tool that mimic's tar's multivolume functionality, but allows you to dynamically change the archive file path when a volume fills up. Unlike tar, this tool should not hold a lock on a particular file, so you can even use this on thumbdrives, external hard drives, or any file-like object
