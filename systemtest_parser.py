import re

class GameObject:
    def __init__(self):
        self.name = ""
        self.position = ""
        self.rotation = ""
        self.uuid = ""
        self.children = []

    def __eq__(self, value):
        return self.name == value.name and self.position == value.position and self.rotation == value.rotation and self.uuid == value.uuid and len(self.children) == len(value.children)
        
    def __ne__(self, value):
        return not __eq__(self, value)

    def __str__(self):
        result = ""
        #result += "--GameObject:\n"
        result += "Name: " + self.name + "\n"
        result += "Position: " + self.position + "\n"
        result += "Rotation: " + self.rotation + "\n"
        result += "UUID: " + self.uuid + "\n"
        result += "Children: " + str(len(self.children))
        return result


def parse_scene(scene):
    regex = re.compile(r'--GameObject:(?:(?!(?:^--|Scene dump end))[\s\S])*', re.MULTILINE)
    objects = regex.findall(scene)
    result = []
    for obj in objects:
        result.append(parse_object(obj))
    return result

def parse_object(obj):
    obj_members = obj.split("Children: ", 1)[0]
    obj_children = obj.split("Children: ", 1)[1]

    result = GameObject()

    # parse members
    for line in obj_members.splitlines():
        pair = list(map(lambda x: x.strip(), line.split(":")))
        if pair[0] == 'Name':
            result.name = pair[1]
        elif pair[0] == 'Position':
            result.position = pair[1]
        elif pair[0] == 'Rotation':
            result.rotation = pair[1]
        elif pair[0] == 'UUID':
            result.uuid = pair[1]

    # format children block
    obj_children_stripped = ""
    for line in obj_children.splitlines()[1:]:
        obj_children_stripped += line[4:] + "\n"
    
    # parse children
    regex = re.compile(r'--GameObject:(?:(?!^--)[\s\S])*', re.MULTILINE)
    children = regex.findall(obj_children_stripped)
    for child in children:
        result.children.append(parse_object(child))
    
    return result
