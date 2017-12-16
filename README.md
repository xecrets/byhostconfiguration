# byhostconfiguration

Maintain different Web.Config configurations for different computers / servers in one single Web.Config, and determine the correct values in run-time.

Implements a host-specific ProtectedConfigurationProvider, which may return different data and have different behavior depending on the actual host that is running the code. This is intended to make it easier to maintain and deploy code between development and different deployment scenarios.

The implementation piggy-backs the ProtectedConfigurationProvider infrastructure. In the future it should also support nesting with a 'Real' ProtectedConfigurationProvider so that it can actually protect as well.

For Visual Studio 2012 (and possibly later) you need to install the assembly in the Global Assembly Cache.

This may be somewhat obsoleted by working web.config transforms, but this is still used in some places and also it might be interesting for other reasons.
