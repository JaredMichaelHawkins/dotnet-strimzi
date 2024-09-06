#!/usr/bin/env bash
set -o errexit
# Replace these with your actual paths and values
registry='localhost:5050'
path='apps'

# function for building and pushing docker image function
build_push_docker() {
    dockerfile_path=$1
    image_name=$2
    tag=$3
    # check if there are changes in the directory
    diff=$(git diff --quiet && echo "No Changes" || echo "Modified")

    # if there are no changes, just print the message and return
    if [ "$diff" == "No Changes" ]; then
        echo "✔ no changes detected in $dockerfile_path"
        return
    fi

    echo "Changes detected, building the Docker image"

    docker build -t $registry/$image_name:$tag -f $dockerfile_path $path
    # catch the last exit code
    build_exit_code=$?

    # if the build failed then exit the script
    if [ $build_exit_code -ne 0 ]; then
        echo "Docker build failed, please check your Dockerfile"
        exit 1
    fi

    echo "Pushing the Docker image to the local registry"
    docker push $registry/$image_name:$tag 
    push_exit_code=$?

    if [ $push_exit_code -ne 0 ]; then
        echo "Failed to push the image to the registry"
        exit 1
    fi

    echo "✔ pushed image to $registry/$image_name:$tag"
}

build_push_docker "${path}/Producer/Dockerfile" "producer" "latest"
build_push_docker "${path}/Consumer/Dockerfile" "consumer" "latest"
