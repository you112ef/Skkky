# DeepSORT Model for Sperm Tracking - README

This directory should contain the DeepSORT model files for sperm tracking in CASA analysis.

## Required Files

### 1. deep_sort_features.onnx
- **Description**: Feature extraction model for object re-identification
- **Format**: ONNX (Open Neural Network Exchange)  
- **Size**: Approximately 20-50 MB
- **Purpose**: Extract appearance features for tracking consistency

### 2. How to Obtain the Model

#### Option 1: Download Official DeepSORT Model
```bash
# Download from official repository
wget https://github.com/nwojke/deep_sort/releases/download/v1.0/mars-small128.pb

# Convert TensorFlow model to ONNX (requires tf2onnx)
pip install tf2onnx
python -m tf2onnx.convert --saved-model mars-small128.pb --output deep_sort_features.onnx
```

#### Option 2: Use Ultralytics Implementation
```bash
pip install ultralytics
python -c "
from ultralytics import YOLO
# This automatically includes DeepSORT tracking
model = YOLO('yolov8n.pt')
# Export with tracking capabilities
results = model.track(source='video.mp4', tracker='deepSORT.yaml')
"
```

#### Option 3: Custom Training for Medical Images
For optimal performance with sperm microscopy:

```python
# Example training script for custom feature extractor
import torch
import torch.nn as nn

class SpermFeatureExtractor(nn.Module):
    def __init__(self):
        super().__init__()
        # Custom CNN architecture for sperm appearance features
        # Designed for small, elongated objects with head/tail structure
        pass
    
    def forward(self, x):
        # Extract 512-dimensional features
        return features

# Train on sperm image patches
# Export to ONNX format
```

### 3. Model Architecture Requirements

The feature extraction model should:
- **Input**: RGB images of size 64x128 pixels
- **Output**: 512-dimensional feature vectors
- **Architecture**: CNN-based (ResNet, EfficientNet, or custom)
- **Training**: Triplet loss or similar metric learning approach

### 4. Tracking Performance Metrics

Expected performance for sperm tracking:
- **Track Consistency**: >95% for well-focused sperm
- **ID Switches**: <2% during normal swimming
- **Track Length**: Average 50+ frames for motile sperm
- **Processing Speed**: Real-time (25-30 FPS)

### 5. CASA-Specific Optimizations

For medical accuracy, the tracking system should handle:
- **Morphology Variations**: Different sperm shapes and sizes
- **Motion Patterns**: Swimming, oscillatory, and irregular movements  
- **Occlusions**: Sperm crossing paths or overlapping
- **Scale Changes**: Focus variations during recording
- **Background Noise**: Debris and other particles

### 6. Alternative Tracking Methods

If DeepSORT is not suitable:

#### SORT (Simple Online Realtime Tracking)
- Lighter weight, faster processing
- Based on Kalman filters and Hungarian algorithm
- Good for simple tracking scenarios

#### ByteTrack
- State-of-the-art performance
- Handles low-score detections well
- Better for crowded scenes

#### FairMOT
- Joint detection and tracking
- Single-shot approach
- Good balance of speed and accuracy

### 7. Configuration Options

Key parameters to tune for sperm tracking:

```yaml
# Tracking sensitivity
max_dist: 0.2          # Lower for stricter matching
min_confidence: 0.3    # Adjust based on detection quality
max_age: 30           # Frames to keep lost tracks
n_init: 3             # Detections needed to start track

# CASA specific
max_velocity: 200.0    # μm/s, filters out noise
min_track_length: 10   # Minimum frames for analysis
```

### 8. Validation and Testing

Before clinical use:
- **Manual Verification**: Compare with expert manual tracking
- **Synthetic Data**: Test on computer-generated sperm movements
- **Cross-Validation**: Multiple samples and operators
- **WHO Standards**: Verify compliance with CASA guidelines

### 9. Integration with CASA Analysis

The tracking output provides:
- **Trajectories**: X,Y coordinates over time
- **Velocities**: VCL, VSL, VAP calculations
- **Motion Parameters**: LIN, STR, WOB, ALH, BCF
- **Classification**: Progressive, non-progressive, immotile

### 10. Performance Optimization

For better performance:
- **GPU Acceleration**: Use CUDA/OpenCL if available
- **Batch Processing**: Process multiple frames together
- **Memory Management**: Efficient buffer handling
- **Multi-threading**: Parallel detection and tracking

## File Structure
```
DeepSORT/
├── deep_sort_features.onnx    # Main feature extraction model
├── kalman_filter.py           # Kalman filter implementation
├── nn_matching.py             # Nearest neighbor matching
├── tracker.py                 # Main tracking logic
└── detection.py               # Detection data structures
```

## Troubleshooting

**Model not loading**: Check ONNX model compatibility and file corruption.

**Poor tracking**: Adjust confidence thresholds and distance metrics.

**ID switches**: Increase feature extraction quality or reduce max_dist parameter.

**Performance issues**: Consider lighter models or optimize inference pipeline.

## Legal Considerations
- Ensure compliance with original DeepSORT license
- Validate for medical device regulations
- Consider intellectual property of tracking algorithms
- Follow data privacy requirements for patient samples