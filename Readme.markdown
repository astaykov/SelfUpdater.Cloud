# A Windows Azure Self Updating Web Role #

This is a sample project that uses dynamic continious update of a WebRole project, 
in the case when it is ASP.NET (or ASP.NET MVC) application.
It utilizies Windows Azure Storage, Windows Azure Service Runtime RoleEntryPoint and WebServerManager (IIS management).

## System architecture ##

There is one core class library - SelfUpdater.Core which does everything. It has a custom RoleEntryPoint implementation.
In normal usage of Windows Azure WebRole you do not need to do anything but reference that library, set it as netFxEntryPoint,
And configure it.

## Required Configuration ##

In order to work properly, SelfUpdater.Core will expect following configuration settings for your Role:
* SelfUpdater.Core.StorageConnectionString - This is Azure Storage account connection string. It will be used to establish connection to the storage
* SelfUpdater.Core.BlobContainer - this is the name of the main container where the new assemblies will be uploaded by developers/administrators
* SelfUpdater.Core.BlobContainerLeasing - this is the name of blob container which will be used for locking
* SelfUpdater.Core.CheckIntervalInSeconds - interval in seconds to check for new versions of the Assemblies

## How it works ##

### Entry Point registration ###
The first step is to register SelfUpdater.Core as RoleEntryPoint in the __.csdef__ file:
		<Runtime executionContext="elevated">
			<EntryPoint>
			<NetFxEntryPoint assemblyName="SelfUpdater.Core.dll" />
			</EntryPoint>
		</Runtime>
Please not that the __elevated__ execution context. It is required because the process will stop IIS Site, copy files to the BIN folder and then start the IIS site again.

### Update checks ###
There is a process, which is being executed every __N__ seconds. You configure this in the Service Configuration 
file by seting the SelfUpdater.Core.CheckIntervalInSeconds property. Upon execution the process will try to ackuire a lease 
on a system blob. Getting a lease will assure that only one role instance is being stopped at a time and avoid full
application down time. If the lease is not successfully acquired, next interval is being waited.

If the lease is acquired successfully, the main IIS Site that serves the Site is stopped. Role Instance begin reporting Busy state
to avoid Load Balanced traffic being sent to the stopped IIS site.
Next the BIN folder of the site is being traversed. For all files found there, a call to Azure Storage is being made
using the __SelfUpdater.Core.BlobContainer__ setting. If file with same name if found, an MD5 check is performed 
to match local file content with Blob content. If content does not match, the Blob is downloaded and overwrites local file.
After all files are processed, the initial blob lease is released, IIS site is started and role instance begin reporting Ready.

__NOTE__
It is really important to stop the IIS Site. It is well known that IIS is monitoring the Bin folder 
(along with web.config & other .config files) for changes and will reset the worker process when such
change occur. However we might have to change multiple assemblies. Imagine one assembly is being replaced,
IIS detecs that and takes action on reseting the worker process. While the worker process is down we change
the next assembly. Now the process is started. IIS does not know that we've changed another assembly.
That's why it is good to stop the IIS site, change everything what is required, then start it again.

Also, because the process does not know whether there will be new updates it stops the IIS site in advance. If there
no new assemblies, it just starts the IIS Site again. This happens on every cycle (and when lease is successfully acquired).
So, it is up to us to tune this interval to avoid too often application performance degradation.

## Additional Solution Items ##
There are two more solution projects - __SelfUpdater.Contracts__ and __SelfUpdater.Impl1__. 
These are just a sample Contract (Interface) and Implementation for testing purposes.
You can run the application as is. Then change the Impl1, compile it, put the new assembly 
into the designated blob container and wait to see the changes on your live application.

## Things to know ##
As the project directly replaces assemblies in Bin folder you should know the following:
This will only work if you:
* do not change the assembly version
OR
* make sure you do not have hard references (specific version = true)

## Future steps ##
An addition and ability to update the web.config will be extremely helpful and will be the next step of the project.
This will be even easier, because there will not be a need for stopping the IIS Site.