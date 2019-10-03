# Agents
2 Agents ( Simulator &amp; Scheduler)  + 2 Lambda Functions ( Message Transformer+Gateway Negotiator, Push Notification Dispatcher - API Authorizer)

#Description

**Simulator Agent:** This agent is a simulator for an autonmous vehicle which is sending its status on a regular basis (lets say every 30 seconds)

**Scheduler Agent:** This agent is responsible for checking if the specific vehicle has sent its status on due time or not, if not this agent will send a "Disconnected" Status for that specific vehicle, this is done via a queueing system so that by getting the first status from any vehicle this agent will schedule the next expected status for that vehicle plus a tolerance amont of time.
