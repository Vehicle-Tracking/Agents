#!/usr/bin/env bash
set -eo pipefail
# more bash-friendly output for jq
JQ="jq --raw-output --exit-status"

TASK_NAME=$1
SERVICE_NAME=$2
ARG_TSK=$3

echo "*** script initiating we received these args {$TASK_NAME} {$SERVICE_NAME} {$ARG_TSK} ... ***"  

export CLUSTERS=$(aws ecs list-clusters | grep $ECS_CLUSTER_NAME || aws ecs create-cluster --cluster-name $ECS_CLUSTER_NAME)

echo "*** ecs {$TASM_NAME} task initiating ... ***"  

export TASK_VERSION=$(aws ecs register-task-definition --family $TASK_NAME --requires-compatibilities FARGATE --cpu 256 --memory 1024 --network-mode awsvpc --container-definitions '[{"name":"$DKR_IMG","image":"$DKR_IMG:latest","entryPoint":["dotnet", "Avt.Agents.Services.dll","--ServiceName {$ARG_TSK}"],"memory":512,"memoryReservation":512,"portMappings":[{"containerPort":18018}],"workingDirectory":"usr/ app","disableNetworking":false,"privileged":true,"healthCheck":{"command":[""],"interval":300,"timeout":60,"retries":5,"startPeriod":150}}]' | jq --raw-output '.taskDefinition.revision')

echo "*** Task Definition *** > " $TASK_NAME:$TASK_VERSION 

export SERVICES=$(aws ecs list-services --cluster $ECS_CLUSTER_NAME  | grep $SERVICE_NAME || aws ecs create-service --service-name $SERVICE_NAME --cluster $ECS_CLUSTER_NAME --task-definition $TASK_NAME  --desired-count 1)

echo "*** creating service and task for {$TASK_NAME} ... ***" 

export DEPLOYED_SERVICE=$(aws ecs update-service --cluster $ECS_CLUSTER_NAME --service $SERVICE_NAME --task-definition $TASK_NAME:$TASK_VERSION  | jq --raw-output  '.service.serviceName')

echo "Deployment of {$DEPLOYED_SERVICE} complete"