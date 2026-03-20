#!/bin/bash
# init-mongo.sh
mongosh -u "$MONGO_INITDB_ROOT_USERNAME" -p "$MONGO_INITDB_ROOT_PASSWORD" --authenticationDatabase admin <<EOF
  rs.initiate({
    _id: 'rs0',
    members: [{ _id: 0, host: 'mongodb:27017' }]
  });
EOF
